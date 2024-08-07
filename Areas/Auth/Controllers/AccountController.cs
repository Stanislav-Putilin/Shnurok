using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using shnurok.Models.ApiResponse;
using shnurok.Services.CosmosDb;
using shnurok.Areas.Auth.Models.Form;
using System.Text.RegularExpressions;
using shnurok.Services.Kdf;

namespace shnurok.Areas.Auth.Controllers
{
    [Area("Auth")]
    [Route("api/auth")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IContainerProvider _containerProvider;
		private readonly IKdfService _kdfService;

		public AccountController(IContainerProvider containerProvider, IKdfService kdfService)
		{
			_containerProvider = containerProvider;
			_kdfService = kdfService;
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

			restResponse.status = new Status { code = 0, message = "все хорошо" };
			return restResponse;
		}
    }
}