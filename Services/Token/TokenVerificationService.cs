using Microsoft.Azure.Cosmos;
using shnurok.Services.CosmosDb;
using shnurok.Services.Hash;

namespace shnurok.Services.Token
{
	public class TokenVerificationService : ITokenVerificationService
	{
		private readonly IContainerProvider _containerProvider;

		public TokenVerificationService(IContainerProvider containerProvider)
		{			
			_containerProvider = containerProvider;
		}
		public async Task<bool> TokenIsValid(string token)
		{
			var container = await _containerProvider.GetContainerAsync();

			var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @token")
								.WithParameter("@token", token);

			using (FeedIterator<shnurok.Models.Db.Token> resultSet = container.GetItemQueryIterator<shnurok.Models.Db.Token>(query))
			{
				if (resultSet.HasMoreResults)
				{
					FeedResponse<shnurok.Models.Db.Token> response = await resultSet.ReadNextAsync();

					if (response.Count == 1)
					{
						var existingToken = response.FirstOrDefault();

						if (existingToken != null)
						{
							if (existingToken.Expires > DateTime.Now)
							{
								return true;
							}
						}
					}
				}
			}

			return false;
		}
	}
}