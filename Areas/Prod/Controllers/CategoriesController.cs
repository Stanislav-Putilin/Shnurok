using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using shnurok.Areas.Auth.Models.Form;
using shnurok.Areas.Prod.Models.Db;
using shnurok.Models.ApiResponse;
using shnurok.Services.CosmosDb;
using shnurok.Services.Kdf;
using System.Text.RegularExpressions;
using shnurok.Areas.Prod.Models.Form;
using shnurok.Models.Db;

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

			var query = new QueryDefinition("SELECT c.id, c.name, c.description FROM c WHERE c.partitionKey = 'categories'");
			var container = await _containerProvider.GetContainerAsync();

			using (FeedIterator<Category> resultSet = container.GetItemQueryIterator<Category>(query))
			{
				if (resultSet.HasMoreResults)
				{
					FeedResponse<Category> response = await resultSet.ReadNextAsync();
					if (response.Count > 0)
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

			var query = new QueryDefinition("SELECT c.categoryId, c.name, c.description FROM c WHERE c.id = @categoryId AND c.partitionKey = 'categories'")
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

		[HttpPost("createcategories")]
		public async Task<RestResponse> CreateCategory([FromHeader(Name = "Authorization")] string token, [FromBody] CategoryCreateForm form)
		{
			RestResponse restResponse = new()
			{
				meta = new()
				{
					{ "endpoint", "api/prod/createcategories" },
					{ "time", DateTime.Now.Ticks },
				}
			};

			if (string.IsNullOrEmpty(token))
			{
				restResponse.status = new Status { code = 8, message = "Пустой токен" };
				restResponse.data = token;
				return restResponse;
			}

			if (!await TokenIsValid(token))
			{
				restResponse.status = new Status { code = 8, message = "Неверный или отсутствует токен" };
				restResponse.data = token;
				return restResponse;
			}

			if (string.IsNullOrWhiteSpace(form.Name))
			{
				restResponse.status = new Status { code = 9, message = "Имя категории не может быть пустым" };
				return restResponse;
			}

			var container = await _containerProvider.GetContainerAsync();

			var query = new QueryDefinition("SELECT * FROM c WHERE c.name = @name AND c.partitionKey = 'categories'")
							.WithParameter("@name", form.Name);

			using (FeedIterator<Category> resultSet = container.GetItemQueryIterator<Category>(query))
			{
				if (resultSet.HasMoreResults)
				{
					FeedResponse<Category> response = await resultSet.ReadNextAsync();
					var existingCategory = response.FirstOrDefault();

					// Если такая категория уже существует
					if (existingCategory != null)
					{
						restResponse.status = new Status { code = 10, message = "Категория с таким именем уже существует" };
						return restResponse;
					}
				}
			}

			Category newCategory = new(Guid.NewGuid().ToString(), form.Name, form.Description, form.ImgUrl);
			
			try
			{
				ItemResponse<Category> response = await container.CreateItemAsync(newCategory, new PartitionKey(newCategory.PartitionKey));
				restResponse.status = new Status { code = 0, message = "Категория успешно создана" };
				restResponse.data = newCategory;
			}
			catch (CosmosException ex)
			{
				restResponse.status = new Status { code = ex.StatusCode == System.Net.HttpStatusCode.Conflict ? 409 : 500, message = ex.Message };
			}

			return restResponse;
		}

		private async Task<bool> TokenIsValid(string token)
		{
			var container = await _containerProvider.GetContainerAsync();
			
			var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @token")
								.WithParameter("@token", token);
			
			using (FeedIterator<Token> resultSet = container.GetItemQueryIterator<Token>(query))
			{
				if (resultSet.HasMoreResults)
				{
					FeedResponse<Token> response = await resultSet.ReadNextAsync();					

					if(response.Count == 1)
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