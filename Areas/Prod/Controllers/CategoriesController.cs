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

		[HttpGet("categories/{categoryId}")]
		public async Task<RestResponse> GetCategoryById(string categoryId)
		{
			var restResponse = new RestResponse
			{
				meta = new Dictionary<string, object>
		{
			{ "endpoint", $"api/prod/categories/{categoryId}" },
			{ "time", DateTime.Now.Ticks },
		}
			};

			var query = new QueryDefinition("SELECT c.categoryId, c.name, c.description FROM c WHERE c.categoryId = @categoryId AND c.partitionKey = 'categories'")
							.WithParameter("@categoryId", categoryId);
			var container = await _containerProvider.GetContainerAsync();

			using (FeedIterator<Category> resultSet = container.GetItemQueryIterator<Category>(query))
			{
				var categories = new List<Category>();

				while (resultSet.HasMoreResults)
				{
					FeedResponse<Category> response = await resultSet.ReadNextAsync();
					categories.AddRange(response);
				}

				if (categories.Count == 1)
				{
					restResponse.status = new Status { code = 0, message = "Категория найдена" };
					restResponse.data = categories.First();
				}
				else if (categories.Count == 0)
				{
					restResponse.status = new Status { code = 6, message = "Категория не найдена" };
				}
				else
				{
					restResponse.status = new Status { code = 7, message = "Найдено несколько категорий с одинаковым идентификатором" };
				}
			}

			return restResponse;
		}
	}
}