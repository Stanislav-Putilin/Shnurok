using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using shnurok.Areas.Auth.Models.Form;
using shnurok.Areas.Prod.Models.Db;
using shnurok.Models.ApiResponse;
using shnurok.Services.CosmosDb;
using shnurok.Services.Kdf;
using System.Text.RegularExpressions;

namespace shnurok.Areas.Prod.Controllers
{
	[Area("Prod")]
	[Route("api/prod")]
	[ApiController]
	public class CategoriesController : ControllerBase
	{
		private readonly IContainerProvider _containerProvider;

		public CategoriesController(IContainerProvider containerProvider)
		{
			_containerProvider = containerProvider;
		}

		[HttpGet("categories")]
		public async Task<RestResponse> GetCategories()
		{
			RestResponse restResponse = new()
			{
				meta = new()
				{
					{ "endpoint", "api/prod/categories" },
					{ "time", DateTime.Now.Ticks },
				}
			};			

			var query = new QueryDefinition("SELECT c.categoryId, c.name, c.description FROM c WHERE c.partitionKey = 'categories'");
			var container = await _containerProvider.GetContainerAsync();

			using (FeedIterator<Category> resultSet = container.GetItemQueryIterator<Category>(query))
			{
				if (resultSet.HasMoreResults)
				{
					FeedResponse<Category> responseEmail = await resultSet.ReadNextAsync();
					if (responseEmail.Count > 0)
					{
						restResponse.status = new Status { code = 0, message = "все хорошо" };						
					}
					else
					{
						restResponse.status = new Status { code = 5, message = "категории еще не добавлены" };
					}
				}
			}
			
			FeedIterator<Category> queryResultSetIterator =
				container.GetItemQueryIterator<Category>(query);

			List<Category> сategories = new();

			while (queryResultSetIterator.HasMoreResults)
			{
				FeedResponse<Category> currentResultSet =
					await queryResultSetIterator.ReadNextAsync();
				foreach (Category сategory in currentResultSet)
				{
					сategories.Add(сategory);
				}
			}

			restResponse.data = сategories;

			return restResponse;
		}
	}
}