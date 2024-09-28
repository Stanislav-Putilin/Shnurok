using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using shnurok.Models.ApiResponse;
using shnurok.Models.Db;
using shnurok.Services.CosmosDb;
using shnurok.Services.Token;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace shnurok.Areas.User
{
	[Area("User")]
	[Route("api/user")]
	[ApiController]
	public class UserController : ControllerBase
	{
		private readonly ITokenVerificationService _tokenVerificationService;
		private readonly IContainerProvider _containerProvider;

		public UserController(ITokenVerificationService tokenVerificationService, IContainerProvider containerProvider)
		{
			_tokenVerificationService = tokenVerificationService;
			_containerProvider = containerProvider;
		}

		[HttpGet("getaddresses")]
		public async Task<RestResponse> GetAddresses()
		{
			RestResponse restResponse = new()
			{
				meta = new()
				{
					{ "endpoint", "api/user/getaddresses" },
					{ "time", DateTime.Now.Ticks },
				}
			};

			if (!Request.Headers.TryGetValue("Authorization", out var tokenHeader))
			{
				restResponse.status = new Status { code = 5, message = "Токен не найден в заголовке" };
				return restResponse;
			}

			var tokenId = tokenHeader.ToString().Split(' ').Last();

			if (string.IsNullOrEmpty(tokenId))
			{
				restResponse.status = new Status { code = 8, message = "Пустой токен" };
				return restResponse;
			}

			if (!await _tokenVerificationService.TokenIsValid(tokenId))
			{
				restResponse.status = new Status { code = 8, message = "Неверный или отсутствует токен" };
				return restResponse;
			}
			
			var container = await _containerProvider.GetContainerAsync();
			var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @token")
						.WithParameter("@token", tokenId);

			using (FeedIterator<Token> resultSet = container.GetItemQueryIterator<Token>(query))
			{
				if (resultSet.HasMoreResults)
				{
					FeedResponse<Token> response = await resultSet.ReadNextAsync();
					var dbToken = response.FirstOrDefault();

					if (dbToken == null)
					{
						restResponse.status = new Status { code = 8, message = "Токен не найден в базе данных" };
						return restResponse;
					}

					Guid userId = dbToken.UserId;
					
					var userInfoQuery = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId AND c.partitionKey = 'userAdditionalInfo'")
						.WithParameter("@userId", userId);

					using (FeedIterator<UserAdditionalInfo> userInfoResultSet = container.GetItemQueryIterator<UserAdditionalInfo>(userInfoQuery))
					{
						UserAdditionalInfo? userInfo = null;

						if (userInfoResultSet.HasMoreResults)
						{
							FeedResponse<UserAdditionalInfo> userInfoResponse = await userInfoResultSet.ReadNextAsync();
							userInfo = userInfoResponse.FirstOrDefault();
						}
						
						if (userInfo == null)
						{
							userInfo = new UserAdditionalInfo
							{
								Id = Guid.NewGuid(),
								UserId = userId,
								Addresses = new List<string>()
							};
							
							await container.CreateItemAsync(userInfo, new PartitionKey(userInfo.PartitionKey));
							restResponse.data = userInfo.Addresses;
							restResponse.status = new Status { code = 0, message = "Информация о пользователе создана" };
						}
						else
						{
							restResponse.status = new Status { code = 0, message = "Адреса успешно получены" };
							restResponse.data = userInfo.Addresses;
						}
					}
				}
				else
				{
					restResponse.status = new Status { code = 8, message = "Неверный или отсутствует токен" };
				}
			}

			return restResponse;
		}
	}
}