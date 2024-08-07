using Microsoft.Azure.Cosmos;

namespace shnurok.Services.CosmosDb
{
	public class NoSqlContainerProvider : IContainerProvider
	{
		private readonly IConfiguration _configuration;
		private Container _container = null!;

		public NoSqlContainerProvider(IConfiguration configuration)
		{
			_configuration = configuration;			
        }

		public async Task<Container> GetContainerAsync()
		{
			if (_container == null)
			{
				try
				{
					var azureSection = _configuration.GetSection("Azure");
					var dbSection = azureSection?.GetSection("CosmosDb");
					var key = dbSection?.GetSection("Key").Value;
					var endpoint = dbSection?.GetSection("Endpoint").Value;
					var databaseId = dbSection?.GetSection("DatabaseId").Value;
					var containerId = dbSection?.GetSection("ContainerId").Value;

					if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(databaseId) || string.IsNullOrEmpty(containerId))
					{
						throw new InvalidOperationException("Один или несколько параметров конфигурации отсутствуют.");
					}

					CosmosClient cosmosClient = new(endpoint, key, new CosmosClientOptions
					{
						ApplicationName = "shnurok"
					});

					Database database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
					_container = await database.CreateContainerIfNotExistsAsync(containerId, "/partitionKey");
				}
				catch (CosmosException ex)
				{					
					Console.WriteLine($"Ошибка при работе с Cosmos DB: {ex.Message}");					
				}
				catch (InvalidOperationException ex)
				{					
					Console.WriteLine($"Ошибка конфигурации: {ex.Message}");					
				}
				catch (Exception ex)
				{					
					Console.WriteLine($"Неизвестная ошибка: {ex.Message}");					
				}
			}

			return _container;
		}
	}
}