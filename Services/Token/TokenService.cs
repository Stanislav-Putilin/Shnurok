using shnurok.Services.CosmosDb;
using shnurok.Services.Hash;
using Microsoft.Azure.Cosmos;

namespace shnurok.Services.Token
{
    public class TokenService : ITokenService
    {
        private readonly IHashService _hashService;
        private readonly IContainerProvider _containerProvider;       

        public TokenService(IHashService hashService, IContainerProvider containerProvider)
        {
            _hashService = hashService;
            _containerProvider = containerProvider;            
        }
        public async Task<string> CreateTokenAsync(Guid userId)
        {
            var container = await _containerProvider.GetContainerAsync();

            Models.Db.Token token = new()
            {
                Id = _hashService.Digest(Guid.NewGuid().ToString()),
                UserId = userId,
                Issued = DateTime.Now,
                Expires = DateTime.Now.AddSeconds(30.0),
            };

            ItemResponse<Models.Db.Token> response = await container.CreateItemAsync(
                token,
                new PartitionKey(token.PartitionKey));            

            return response.Resource.Id;
        }
    }
}