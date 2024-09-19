using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using shnurok.Areas.Orders.Models.Db;
using shnurok.Models.ApiResponse;
using shnurok.Services.CosmosDb;
using shnurok.Services.Token;
using Newtonsoft.Json;
using shnurok.Models.Db;
using Azure.Core;
using shnurok.Areas.Prod.Models.Db;

namespace shnurok.Areas.Orders.Controllers
{
	[Area("Orders")]
	[Route("api/orders")]
	[ApiController]
	public class OrdersController : ControllerBase
	{
		private readonly IContainerProvider _containerProvider;
		private readonly ITokenVerificationService _tokenVerificationService;

		public OrdersController(IContainerProvider containerProvider, ITokenVerificationService tokenVerificationService)
		{
			_containerProvider = containerProvider;
			_tokenVerificationService = tokenVerificationService;
		}

		//[HttpPost("createorder")]
		//public async Task<RestResponse> CreateOrder([FromBody] Order form)
		//{
		//	RestResponse restResponse = new()
		//	{
		//		meta = new()
		//		{
		//			{ "endpoint", "api/orders/createorder" },
		//			{ "time", DateTime.Now.Ticks },
		//		}
		//	};

		//	// Получение токена из заголовка
		//	if (!Request.Headers.TryGetValue("Authorization", out var tokenHeader))
		//	{
		//		restResponse.status = new Status { code = 5, message = "Токен не найден в заголовке" };
		//		return restResponse;
		//	}

		//	var tokenId = tokenHeader.ToString().Split(' ').Last();

		//	// Проверка токена
		//	if (string.IsNullOrEmpty(tokenId))
		//	{
		//		restResponse.status = new Status { code = 8, message = "Пустой токен" };
		//		restResponse.data = tokenId;
		//		return restResponse;
		//	}

		//	if (!await _tokenVerificationService.TokenIsValid(tokenId))
		//	{
		//		restResponse.status = new Status { code = 8, message = "Неверный или отсутствует токен" };
		//		restResponse.data = tokenId;
		//		return restResponse;
		//	}

		//	// Проверка формы заказа
		//	//if (string.IsNullOrWhiteSpace(form.CustomerId) || !form.Items.Any())
		//	//{
		//	//	restResponse.status = new Status { code = 9, message = "Заказ должен содержать клиента и хотя бы один товар" };
		//	//	return restResponse;
		//	//}

		//	// Создание заказа
		//	form.Id = Guid.NewGuid();
		//	form.PartitionKey = "orders";

		//	var container = await _containerProvider.GetContainerAsync();

		//	try
		//	{
		//		ItemResponse<Order> response = await container.CreateItemAsync(form, new PartitionKey(form.PartitionKey));
		//		restResponse.status = new Status { code = 0, message = "Заказ успешно создан" };
		//		restResponse.data = form;
		//	}
		//	catch (CosmosException ex)
		//	{
		//		restResponse.status = new Status
		//		{
		//			code = ex.StatusCode == System.Net.HttpStatusCode.Conflict ? 409 : 500,
		//			message = ex.Message
		//		};
		//	}

		//	return restResponse;
		//}

		[HttpPost("additemfromcart")]
		public async Task<RestResponse> AddItemFromCart([FromBody] string itemId)
		{
			RestResponse restResponse = new()
			{
				meta = new()
				{
					{ "endpoint", "api/orders/additemfromcart" },
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
				restResponse.data = tokenId;
				return restResponse;
			}

			if (!await _tokenVerificationService.TokenIsValid(tokenId))
			{
				restResponse.status = new Status { code = 8, message = "Неверный или отсутствует токен" };
				restResponse.data = tokenId;
				return restResponse;
			}

			if (string.IsNullOrWhiteSpace(itemId))
			{
				restResponse.status = new Status { code = 9, message = "Id товара должен быть" };
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

					var cartQuery = new QueryDefinition("SELECT * FROM c WHERE c.customerId = @customerId AND c.partitionKey = 'temporaryCart'")
							.WithParameter("@customerId", userId);

					using (FeedIterator<TemporaryCart> cartResultSet = container.GetItemQueryIterator<TemporaryCart>(cartQuery))
					{
						TemporaryCart? tempCart = null;

						if (cartResultSet.HasMoreResults)
						{
							FeedResponse<TemporaryCart> cartResponse = await cartResultSet.ReadNextAsync();
							tempCart = cartResponse.FirstOrDefault();
						}

						if (tempCart == null)
						{
							tempCart = new TemporaryCart
							{
								Id = Guid.NewGuid(),
								CustomerId = userId,
								Items = new List<OrderItem>
						{
							new OrderItem
							{
								ProductId = itemId,
								Quantity = 1
							}
						}
							};

							await container.CreateItemAsync(tempCart, new PartitionKey(tempCart.PartitionKey));

							restResponse.status = new Status { code = 0, message = "Временная корзина создана и товар добавлен" };
							restResponse.data = tempCart;
						}
						else
						{
							var existingItem = tempCart.Items.FirstOrDefault(item => item.ProductId == itemId);

							if (existingItem != null)
							{
								existingItem.Quantity += 1;
							}
							else
							{
								tempCart.Items.Add(new OrderItem
								{
									ProductId = itemId,
									Quantity = 1
								});
							}

							await container.UpsertItemAsync(tempCart, new PartitionKey(tempCart.PartitionKey));

							restResponse.status = new Status { code = 0, message = "Товар добавлен в корзину" };
							restResponse.data = tempCart;
						}
					}

				}
				else
				{
					restResponse.status = new Status { code = 8, message = "Неверный или отсутствует токен" };
					return restResponse;
				}
			}

			return restResponse;
		}

		[HttpPost("removeitemfromcart")]
		public async Task<RestResponse> RemoveItemFromCart([FromBody] string itemId)
		{
			RestResponse restResponse = new()
			{
				meta = new()
		{
			{ "endpoint", "api/orders/removeitemfromcart" },
			{ "time", DateTime.Now.Ticks },
		}
			};

			// Проверка заголовка Authorization
			if (!Request.Headers.TryGetValue("Authorization", out var tokenHeader))
			{
				restResponse.status = new Status { code = 5, message = "Токен не найден в заголовке" };
				return restResponse;
			}

			var tokenId = tokenHeader.ToString().Split(' ').Last();

			if (string.IsNullOrEmpty(tokenId))
			{
				restResponse.status = new Status { code = 8, message = "Пустой токен" };
				restResponse.data = tokenId;
				return restResponse;
			}

			// Проверка токена
			if (!await _tokenVerificationService.TokenIsValid(tokenId))
			{
				restResponse.status = new Status { code = 8, message = "Неверный или отсутствует токен" };
				restResponse.data = tokenId;
				return restResponse;
			}

			// Проверка наличия ID товара
			if (string.IsNullOrWhiteSpace(itemId))
			{
				restResponse.status = new Status { code = 9, message = "Id товара должен быть" };
				return restResponse;
			}

			var container = await _containerProvider.GetContainerAsync();

			// Получение пользователя по токену
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

					// Получение временной корзины пользователя
					var cartQuery = new QueryDefinition("SELECT * FROM c WHERE c.customerId = @customerId AND c.partitionKey = 'temporaryCart'")
							.WithParameter("@customerId", userId);

					using (FeedIterator<TemporaryCart> cartResultSet = container.GetItemQueryIterator<TemporaryCart>(cartQuery))
					{
						TemporaryCart? tempCart = null;

						if (cartResultSet.HasMoreResults)
						{
							FeedResponse<TemporaryCart> cartResponse = await cartResultSet.ReadNextAsync();
							tempCart = cartResponse.FirstOrDefault();
						}

						if (tempCart == null)
						{
							restResponse.status = new Status { code = 10, message = "Корзина не найдена" };
							return restResponse;
						}
						else
						{
							// Поиск товара в корзине
							var existingItem = tempCart.Items.FirstOrDefault(item => item.ProductId == itemId);

							if (existingItem != null)
							{
								if (existingItem.Quantity > 1)
								{
									existingItem.Quantity -= 1;
								}
								else
								{
									tempCart.Items.Remove(existingItem);
								}

								await container.UpsertItemAsync(tempCart, new PartitionKey(tempCart.PartitionKey));

								restResponse.status = new Status { code = 0, message = "Товар удален из корзины" };
								restResponse.data = tempCart;
							}
							else
							{
								restResponse.status = new Status { code = 11, message = "Товар не найден в корзине" };
							}
						}
					}

				}
				else
				{
					restResponse.status = new Status { code = 8, message = "Неверный или отсутствует токен" };
					return restResponse;
				}
			}

			return restResponse;
		}

		[HttpPost("removeallitemsfromcart")]
		public async Task<RestResponse> RemoveAllItemsFromCart([FromBody] string itemId)
		{
			RestResponse restResponse = new()
			{
				meta = new()
		{
			{ "endpoint", "api/orders/removeallitemsfromcart" },
			{ "time", DateTime.Now.Ticks },
		}
			};

			// Проверка заголовка Authorization
			if (!Request.Headers.TryGetValue("Authorization", out var tokenHeader))
			{
				restResponse.status = new Status { code = 5, message = "Токен не найден в заголовке" };
				return restResponse;
			}

			var tokenId = tokenHeader.ToString().Split(' ').Last();

			if (string.IsNullOrEmpty(tokenId))
			{
				restResponse.status = new Status { code = 8, message = "Пустой токен" };
				restResponse.data = tokenId;
				return restResponse;
			}

			// Проверка токена
			if (!await _tokenVerificationService.TokenIsValid(tokenId))
			{
				restResponse.status = new Status { code = 8, message = "Неверный или отсутствует токен" };
				restResponse.data = tokenId;
				return restResponse;
			}

			// Проверка наличия ID товара
			if (string.IsNullOrWhiteSpace(itemId))
			{
				restResponse.status = new Status { code = 9, message = "Id товара должен быть" };
				return restResponse;
			}

			var container = await _containerProvider.GetContainerAsync();

			// Получение пользователя по токену
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

					// Получение временной корзины пользователя
					var cartQuery = new QueryDefinition("SELECT * FROM c WHERE c.customerId = @customerId AND c.partitionKey = 'temporaryCart'")
							.WithParameter("@customerId", userId);

					using (FeedIterator<TemporaryCart> cartResultSet = container.GetItemQueryIterator<TemporaryCart>(cartQuery))
					{
						TemporaryCart? tempCart = null;

						if (cartResultSet.HasMoreResults)
						{
							FeedResponse<TemporaryCart> cartResponse = await cartResultSet.ReadNextAsync();
							tempCart = cartResponse.FirstOrDefault();
						}

						if (tempCart == null)
						{
							restResponse.status = new Status { code = 10, message = "Корзина не найдена" };
							return restResponse;
						}
						else
						{
							// Удаление всех вхождений товара из корзины
							tempCart.Items.RemoveAll(item => item.ProductId == itemId);

							await container.UpsertItemAsync(tempCart, new PartitionKey(tempCart.PartitionKey));

							restResponse.status = new Status { code = 0, message = "товар полностью удален из корзины" };
							restResponse.data = tempCart;
						}
					}
				}
				else
				{
					restResponse.status = new Status { code = 8, message = "Неверный или отсутствует токен" };
					return restResponse;
				}
			}

			return restResponse;
		}

		[HttpPost("clearcart")]
		public async Task<RestResponse> ClearCart()
		{
			RestResponse restResponse = new()
			{
				meta = new()
		{
			{ "endpoint", "api/orders/clearcart" },
			{ "time", DateTime.Now.Ticks },
		}
			};

			// Проверка заголовка Authorization
			if (!Request.Headers.TryGetValue("Authorization", out var tokenHeader))
			{
				restResponse.status = new Status { code = 5, message = "Токен не найден в заголовке" };
				return restResponse;
			}

			var tokenId = tokenHeader.ToString().Split(' ').Last();

			if (string.IsNullOrEmpty(tokenId))
			{
				restResponse.status = new Status { code = 8, message = "Пустой токен" };
				restResponse.data = tokenId;
				return restResponse;
			}

			// Проверка валидности токена
			if (!await _tokenVerificationService.TokenIsValid(tokenId))
			{
				restResponse.status = new Status { code = 8, message = "Неверный или отсутствует токен" };
				restResponse.data = tokenId;
				return restResponse;
			}

			var container = await _containerProvider.GetContainerAsync();

			// Получение пользователя по токену
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

					// Получение временной корзины пользователя
					var cartQuery = new QueryDefinition("SELECT * FROM c WHERE c.customerId = @customerId AND c.partitionKey = 'temporaryCart'")
							.WithParameter("@customerId", userId);

					using (FeedIterator<TemporaryCart> cartResultSet = container.GetItemQueryIterator<TemporaryCart>(cartQuery))
					{
						TemporaryCart? tempCart = null;

						if (cartResultSet.HasMoreResults)
						{
							FeedResponse<TemporaryCart> cartResponse = await cartResultSet.ReadNextAsync();
							tempCart = cartResponse.FirstOrDefault();
						}

						if (tempCart == null)
						{
							restResponse.status = new Status { code = 10, message = "Корзина не найдена" };
							return restResponse;
						}
						else
						{
							// Очистка всех товаров из корзины
							tempCart.Items.Clear();

							// Обновление корзины в базе данных
							await container.UpsertItemAsync(tempCart, new PartitionKey(tempCart.PartitionKey));

							restResponse.status = new Status { code = 0, message = "Корзина очищена" };
							restResponse.data = tempCart;
						}
					}

				}
				else
				{
					restResponse.status = new Status { code = 8, message = "Неверный или отсутствует токен" };
					return restResponse;
				}
			}
			return restResponse;
		}

		[HttpGet("getcart")]
		public async Task<RestResponse> GetCart()
		{
			RestResponse restResponse = new()
			{
				meta = new()
		{
			{ "endpoint", "api/orders/getcart" },
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
				restResponse.data = tokenId;
				return restResponse;
			}

			if (!await _tokenVerificationService.TokenIsValid(tokenId))
			{
				restResponse.status = new Status { code = 8, message = "Неверный или отсутствует токен" };
				restResponse.data = tokenId;
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

					var cartQuery = new QueryDefinition("SELECT * FROM c WHERE c.customerId = @customerId AND c.partitionKey = 'temporaryCart'")
							.WithParameter("@customerId", userId);

					using (FeedIterator<TemporaryCart> cartResultSet = container.GetItemQueryIterator<TemporaryCart>(cartQuery))
					{
						TemporaryCart? tempCart = null;

						if (cartResultSet.HasMoreResults)
						{
							FeedResponse<TemporaryCart> cartResponse = await cartResultSet.ReadNextAsync();
							tempCart = cartResponse.FirstOrDefault();
						}

						if (tempCart == null)
						{
							restResponse.status = new Status { code = 10, message = "Корзина не найдена" };
							return restResponse;
						}
						else
						{
							var itemsWithProducts = new List<object>();

							foreach (var item in tempCart.Items)
							{
								var productQuery = new QueryDefinition("SELECT * FROM c WHERE c.id = @productId AND c.partitionKey = 'products'")
									.WithParameter("@productId", item.ProductId);

								using (FeedIterator<Product> productResultSet = container.GetItemQueryIterator<Product>(productQuery))
								{
									if (productResultSet.HasMoreResults)
									{
										FeedResponse<Product> productResponse = await productResultSet.ReadNextAsync();
										var product = productResponse.FirstOrDefault();

										if (product != null)
										{
											var productInfo = new
											{
												product.Id,
												product.Name,
												product.Description,
												product.Price,
												product.Discount,
												product.Category,
												product.StockQuantity,
												product.Images,
												product.Tags
											};

											itemsWithProducts.Add(new
											{
												Product = productInfo,
												Quantity = item.Quantity
											});
										}
									}
								}
							}

							var anonymousCart = new
							{
								tempCart.Id,
								tempCart.CustomerId,
								Items = itemsWithProducts
							};

							restResponse.status = new Status { code = 0, message = "Корзина успешно получена" };
							restResponse.data = anonymousCart;
						}
					}

				}
				else
				{
					restResponse.status = new Status { code = 8, message = "Неверный или отсутствует токен" };
					return restResponse;
				}
			}

			return restResponse;
		}
	}
}