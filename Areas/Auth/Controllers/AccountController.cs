using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using shnurok.Models.ApiResponse;
using shnurok.Services.CosmosDb;
using shnurok.Areas.Auth.Models.Form;
using System.Text.RegularExpressions;
using shnurok.Services.Kdf;
using shnurok.Services.Token;
using shnurok.Models.Db;

namespace shnurok.Areas.Auth.Controllers
{
	[Area("Auth")]
	[Route("api/auth")]
	[ApiController]
	public class AccountController : ControllerBase
	{
		private readonly IContainerProvider _containerProvider;
		private readonly IKdfService _kdfService;
		private readonly ITokenService _tokenService;

		public AccountController(IContainerProvider containerProvider, IKdfService kdfService, ITokenService tokenService)
		{
			_containerProvider = containerProvider;
			_kdfService = kdfService;
			_tokenService = tokenService;
		}

		[HttpPost("signup")]
		public async Task<RestResponse> Signup([FromBody] SignupForm form)
		{
			RestResponse restResponse = new()
			{
				meta = new()
				{
					{ "endpoint", "api/auth/signup" },
					{ "time", DateTime.Now.Ticks },
				}
			};

			if (form.UserPassword == null || form.UserPassword != form.UserConfirmPassword)
			{
				restResponse.status = new Status { code = 1, message = "ошибка в пароле" };
				return restResponse;
			}

			string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
			if (form.UserEmail != null)
			{
				if (!Regex.IsMatch(form.UserEmail, emailPattern))
				{
					restResponse.status = new Status { code = 2, message = "ошибка в email" };
					return restResponse;
				}
			}
			else
			{
				restResponse.status = new Status { code = 2, message = "ошибка в email" };
				return restResponse;
			}

			var query = new QueryDefinition("SELECT * FROM c WHERE c.email = @Email").WithParameter("@Email", form.UserEmail);
			var container = await _containerProvider.GetContainerAsync();

			using (FeedIterator<shnurok.Models.Db.User> resultSet = container.GetItemQueryIterator<shnurok.Models.Db.User>(query))
			{
				if (resultSet.HasMoreResults)
				{
					FeedResponse<shnurok.Models.Db.User> responseEmail = await resultSet.ReadNextAsync();
					if (responseEmail.Count > 0)
					{
						restResponse.status = new Status { code = 3, message = "такой email уже зарегистрирован" };
						return restResponse;
					}
				}
			}

			var user = new shnurok.Models.Db.User
			{
				Id = Guid.NewGuid(),
				Email = form.UserEmail,
				Dk = _kdfService.DerivedKey(form.UserPassword)
			};

			ItemResponse<shnurok.Models.Db.User> response = await
				container.CreateItemAsync(
					user,
					new PartitionKey(user.PartitionKey)
				);

			string token = await _tokenService.CreateTokenAsync(user.Id);

			restResponse.data = token;

			restResponse.status = new Status { code = 0, message = "все хорошо" };

			return restResponse;
		}

		[HttpPost("login")]
		public async Task<RestResponse> Login([FromBody] LoginForm form)
		{
			RestResponse restResponse = new()
			{
				meta = new()
				{
					{ "endpoint", "api/auth/login" },
					{ "time", DateTime.Now.Ticks },
				}
			};
			
			QueryDefinition? queryDefinition = new QueryDefinition ($"SELECT * FROM c WHERE c.partitionKey = 'users' AND c.email = @Email").WithParameter("@Email", form.UserEmail);
			var container = await _containerProvider.GetContainerAsync();

			string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

			if (!Regex.IsMatch(form.UserEmail, emailPattern))
			{
				restResponse.status = new Status { code = 4, message = "неверный логин или пароль" };
				return restResponse;
			}

			FeedIterator<shnurok.Models.Db.User> queryResultSetIterator =
				container.GetItemQueryIterator<shnurok.Models.Db.User>(queryDefinition);

			List<shnurok.Models.Db.User> results = new();
			while (queryResultSetIterator.HasMoreResults)
			{
				FeedResponse<shnurok.Models.Db.User> currentResultSet =
					await queryResultSetIterator.ReadNextAsync();
				foreach (shnurok.Models.Db.User family in currentResultSet)
				{
					results.Add(family);
				}
			}

			var user = results.FirstOrDefault();
			if (user != null &&
				_kdfService.DerivedKey(form.UserPassword, user.Dk.Split('.')[0]) == user.Dk)
			{
				string token = await _tokenService.CreateTokenAsync(user.Id);

				restResponse.data = token;

				restResponse.status = new Status { code = 0, message = "все хорошо" };

				return restResponse;
			}
			else
			{
				restResponse.status = new Status { code = 4, message = "неверный логин или пароль" };

				return restResponse;
			}
		}

		[HttpPost("logout")]
		public async Task<RestResponse> Logout()
		{
			RestResponse restResponse = new()
			{
				meta = new()
				{
					{ "endpoint", "api/auth/logout" },
					{ "time", DateTime.Now.Ticks },
				}
			};
						
			if (!Request.Headers.TryGetValue("Authorization", out var tokenHeader))
			{
				restResponse.status = new Status { code = 5, message = "Токен не найден в заголовке" };
				return restResponse;
			}
			
			var tokenId = tokenHeader.ToString().Split(' ').Last();
						
			var container = await _containerProvider.GetContainerAsync();

			try
			{
				
				var tokenResponse = await container.ReadItemAsync<Token>(tokenId, new PartitionKey("tokens"));
				var token = tokenResponse.Resource;
				
				token.Expires = DateTime.UtcNow;
				
				await container.ReplaceItemAsync(token, token.Id, new PartitionKey(token.PartitionKey));

				restResponse.status = new Status { code = 0, message = "Токен успешно деактивирован" };
			}
			catch (CosmosException)
			{
				restResponse.status = new Status { code = 11, message = "Ошибка при обработке токена, при попытке деактивации!" };
			}

			return restResponse;
		}
	}
}