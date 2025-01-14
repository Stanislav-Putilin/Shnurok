﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using shnurok.Areas.Prod.Models.Db;
using shnurok.Models.ApiResponse;
using shnurok.Services.CosmosDb;

namespace shnurok.Areas.Prod.Controllers
{
	[Area("Prod")]
	[Route("api/prod")]
	[ApiController]
	public class ProductsController : ControllerBase
	{
		private readonly IContainerProvider _containerProvider;

		public ProductsController(IContainerProvider containerProvider)
		{
			_containerProvider = containerProvider;
		}

		[HttpGet("allproducts")]
		public async Task<RestResponse> GetProducts()
		{
			var restResponse = new RestResponse
			{
				meta = new Dictionary<string, object>
				{
					{ "endpoint", "api/prod/allproducts" },
					{ "time", DateTime.Now.Ticks },
				}
			};

			var query = new QueryDefinition("SELECT p.id, p.name, p.description, p.price, p.discount, p.category, p.stockQuantity, p.images, p.tags FROM p WHERE p.partitionKey = 'products'");
			var container = await _containerProvider.GetContainerAsync();

			using (FeedIterator<Product> resultSet = container.GetItemQueryIterator<Product>(query))
			{
				var products = new List<Product>();

				while (resultSet.HasMoreResults)
				{
					FeedResponse<Product> response = await resultSet.ReadNextAsync();
					products.AddRange(response);
				}

				if (products.Count > 0)
				{
					restResponse.status = new Status { code = 0, message = "Все хорошо" };
					restResponse.data = products;
				}
				else
				{
					restResponse.status = new Status { code = 6, message = "Товары не найдены" };
				}
			}

			return restResponse;
		}

		[HttpGet("product/{productId}")]
		public async Task<RestResponse> GetProductById(string productId)
		{
			var restResponse = new RestResponse
			{
				meta = new Dictionary<string, object>
				{
					{ "endpoint", $"api/prod/product/{productId}" },
					{ "time", DateTime.Now.Ticks },
				}
			};

			var query = new QueryDefinition("SELECT p.id, p.name, p.description, p.price, p.discount, p.category, p.stockQuantity, p.images, p.tags FROM p WHERE p.id = @productId AND p.partitionKey = 'products'")
							.WithParameter("@productId", productId);
			var container = await _containerProvider.GetContainerAsync();

			using (FeedIterator<Product> resultSet = container.GetItemQueryIterator<Product>(query))
			{
				var products = new List<Product>();

				while (resultSet.HasMoreResults)
				{
					FeedResponse<Product> response = await resultSet.ReadNextAsync();
					products.AddRange(response);
				}

				if (products.Count == 1)
				{
					restResponse.status = new Status { code = 0, message = "Продукт найден" };
					restResponse.data = products.First();
				}
				else if (products.Count == 0)
				{
					restResponse.status = new Status { code = 6, message = "Продукт не найден" };
				}
				else
				{
					restResponse.status = new Status { code = 7, message = "Найдено несколько продуктов с одинаковым идентификатором" };
				}
			}

			return restResponse;
		}

		[HttpGet("carousel")]
		public async Task<RestResponse> GetСarousel()
		{
			var restResponse = new RestResponse
			{
				meta = new Dictionary<string, object>
				{
					{ "endpoint", "api/prod/carousel" },
					{ "time", DateTime.Now.Ticks },
				}
			};

			var imageUrls = new List<string>
			{
				"https://picsum.photos/1200/400?image=1",
				"https://picsum.photos/1200/400?image=2",
				"https://picsum.photos/1200/400?image=3",
				"https://picsum.photos/1200/400?image=4",
				"https://picsum.photos/1200/400?image=5",
				"https://picsum.photos/1200/400?image=6",
				"https://picsum.photos/1200/400?image=7",
				"https://picsum.photos/1200/400?image=8",
				"https://picsum.photos/1200/400?image=9",
				"https://picsum.photos/1200/400?image=10"
			};
			
			restResponse.status = new Status { code = 0, message = "Все хорошо" };
			restResponse.data = imageUrls;			

			return restResponse;
		}

		[HttpGet("categoryproducts/{categoryId}")]
		public async Task<RestResponse> GetProductsByCategory(string categoryId)
		{
			var restResponse = new RestResponse
			{
				meta = new Dictionary<string, object>
				{
					{ "endpoint", $"api/prod/categoryproducts/{categoryId}" },
					{ "time", DateTime.Now.Ticks },
				}
			};

			var query = new QueryDefinition("SELECT p.id, p.name, p.description, p.price, p.discount, p.category, p.stockQuantity, p.images, p.tags FROM p WHERE p.partitionKey = 'products' AND p.category = @categoryId")
				.WithParameter("@categoryId", categoryId);

			var container = await _containerProvider.GetContainerAsync();

			using (FeedIterator<Product> resultSet = container.GetItemQueryIterator<Product>(query))
			{
				var products = new List<Product>();

				while (resultSet.HasMoreResults)
				{
					FeedResponse<Product> response = await resultSet.ReadNextAsync();
					products.AddRange(response);
				}

				if (products.Count > 0)
				{
					restResponse.status = new Status { code = 0, message = "Все хорошо" };
					restResponse.data = products;
				}
				else
				{
					restResponse.status = new Status { code = 6, message = "Товары для данной категории не найдены" };
				}
			}

			return restResponse;
		}

		[HttpGet("search")]
		public async Task<RestResponse> SearchProducts([FromQuery] string query)
		{
			var restResponse = new RestResponse
			{
				meta = new Dictionary<string, object>
		{
			{ "endpoint", "api/prod/search" },
			{ "time", DateTime.Now.Ticks },
		}
			};

			var sqlQuery = new QueryDefinition(
				"SELECT p.id, p.name, p.description, p.price, p.discount, p.category, p.stockQuantity, p.images, p.tags " +
				"FROM p WHERE p.partitionKey = 'products' " +
				"AND (CONTAINS(p.name, @query) OR CONTAINS(p.description, @query) OR ARRAY_CONTAINS(p.tags, @query))"
			).WithParameter("@query", query);

			var container = await _containerProvider.GetContainerAsync();

			using (FeedIterator<Product> resultSet = container.GetItemQueryIterator<Product>(sqlQuery))
			{
				var products = new List<Product>();

				while (resultSet.HasMoreResults)
				{
					FeedResponse<Product> response = await resultSet.ReadNextAsync();
					products.AddRange(response);
				}

				if (products.Count > 0)
				{
					restResponse.status = new Status { code = 0, message = "Продукты найдены" };
					restResponse.data = products;
				}
				else
				{
					restResponse.status = new Status { code = 6, message = "Продукты не найдены" };
				}
			}

			return restResponse;
		}
	}
}