using Microsoft.Azure.Cosmos;

namespace shnurok.Services.CosmosDb
{
    public interface IContainerProvider
    {
        Task<Container> GetContainerAsync();
    }
}
