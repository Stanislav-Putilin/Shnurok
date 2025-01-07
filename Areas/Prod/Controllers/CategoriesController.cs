using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using shnurok.Areas.Auth.Models.Form;
using shnurok.Areas.Prod.Models.Db;
using shnurok.Models.ApiResponse;
using shnurok.Services.CosmosDb;
using shnurok.Services.Dropbox;
using shnurok.Services.Kdf;
using System.Text.RegularExpressions;
using shnurok.Areas.Prod.Models.Form;
using shnurok.Models.Db;
using Azure;
using Newtonsoft.Json.Linq;
using System.Security.Policy;
using shnurok.Services.Token;
using Dropbox.Api;
using Dropbox.Api.Files;

namespace shnurok.Areas.Prod.Controllers
{
	[Area("Prod")]
	[Route("api/prod")]
	[ApiController]
	public class CategoriesController : ControllerBase
	{
		private readonly IContainerProvider _containerProvider;
		private readonly ITokenVerificationService _tokenVerificationService;
		private readonly IContainerImagesProvider _imagesProvider;

		public CategoriesController(IContainerProvider containerProvider,
									ITokenVerificationService tokenVerificationService,
									IContainerImagesProvider imagesProvider)
		{
			_containerProvider = containerProvider;
			_tokenVerificationService = tokenVerificationService;
			_imagesProvider = imagesProvider;
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

			var query = new QueryDefinition("SELECT c.id, c.name, c.description, c.slug, c.imgUrl, c.createdAt, c.deletedAt  FROM c WHERE c.partitionKey = 'categories'");
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
					if(сategory.DeletedAt == null)
					{
						сategories.Add(сategory);
					}					
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
		public async Task<RestResponse> CreateCategory([FromHeader(Name = "Authorization")] string token, [FromForm] CategoryCreateForm form)
		{
			var dropboxClient = await _imagesProvider.GetDropboxClientContainerAsync();

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

			if (!await _tokenVerificationService.TokenIsValid(token))
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

			var query = new QueryDefinition("SELECT * FROM c WHERE (c.name = @name OR c.slug = @slug) AND c.partitionKey = 'categories'")
				.WithParameter("@name", form.Name)
				.WithParameter("@slug", form.Slug);

			using (FeedIterator<Category> resultSet = container.GetItemQueryIterator<Category>(query))
			{
				if (resultSet.HasMoreResults)
				{
					FeedResponse<Category> response = await resultSet.ReadNextAsync();
					var existingCategory = response.FirstOrDefault();

					// Если такая категория уже существует по имени или slug
					if (existingCategory != null)
					{
						restResponse.status = new Status { code = 10, message = "Категория с таким именем или slug уже существует" };
						return restResponse;
					}
				}
			}

			List<string> imageLinks = new List<string>();

			foreach (IFormFile file in form.Images)
			{
				string extension = Path.GetExtension(file.FileName).ToLower();
				if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" ||
					extension == ".gif" || extension == ".bmp" || extension == ".tiff" ||
					extension == ".tif" || extension == ".webp" || extension == ".heic" ||
					extension == ".heif" || extension == ".svg" || extension == ".ico")
				{
					using var fileStream = file.OpenReadStream();
					var fileName = $"/{Guid.NewGuid() + Path.GetExtension(file.FileName).ToLower()}";
					var uploadResult = await dropboxClient.Files.UploadAsync(
						path: fileName,
						WriteMode.Overwrite.Instance,
						body: fileStream);

					var sharedLink = await dropboxClient.Sharing.CreateSharedLinkWithSettingsAsync(uploadResult.PathLower);

					// Преобразуем ссылку в прямую, заменив параметр dl=0 на raw=1
					string directLink = sharedLink.Url.Replace("&dl=0", "&raw=1");
					imageLinks.Add(directLink);
				}
			}

			Category newCategory = new Category
			{
				Id = Guid.NewGuid(),
				Name = form.Name,
				Description = form.Description,
				ImgUrl = imageLinks,
				Slug = form.Slug,
				CreatedAt = DateTime.Now
			};

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


		[HttpPost("updatecategory")]
		public async Task<RestResponse> UpdateCategory([FromHeader(Name = "Authorization")] string token, [FromForm] Guid categoryId, [FromForm] CategoryCreateForm form)
		{
			RestResponse restResponse = new()
			{
				meta = new()
		{
			{ "endpoint", $"api/prod/updatecategory/{categoryId}" },
			{ "time", DateTime.Now.Ticks },
		}
			};
						
			if (string.IsNullOrEmpty(token) || !await _tokenVerificationService.TokenIsValid(token))
			{
				restResponse.status = new Status { code = 8, message = "Неверный или отсутствует токен" };
				restResponse.data = token;
				return restResponse;
			}

			var container = await _containerProvider.GetContainerAsync();
						
			ItemResponse<Category> response;
			try
			{
				response = await container.ReadItemAsync<Category>(categoryId.ToString(), new PartitionKey("categories"));
			}
			catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
			{
				restResponse.status = new Status { code = 404, message = "Категория не найдена" };
				return restResponse;
			}

			Category category = response.Resource;
			var dropboxClient = await _imagesProvider.GetDropboxClientContainerAsync();
						
			if (form.Images != null && form.Images.Count > 0)
			{
				if (category.ImgUrl != null)
				{
					foreach (string oldImageUrl in category.ImgUrl)
					{
						var oldFilePath = new Uri(oldImageUrl).AbsolutePath;
						
						//oldFilePath = "https://www.dropbox.com" + oldFilePath;
						//Console.WriteLine(oldFilePath);
						try
						{
							await dropboxClient.Files.DeleteV2Async(oldFilePath);
						}
						catch (Exception ex)
						{
							restResponse.meta.Add("warning", $"Не удалось удалить файл: {oldFilePath}, причина: {ex.Message}");
						}
					}
				}
				
				List<string> newImageLinks = new List<string>();
				foreach (IFormFile file in form.Images)
				{
					string extension = Path.GetExtension(file.FileName).ToLower();
					if (new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif", ".webp", ".heic", ".heif", ".svg", ".ico" }.Contains(extension))
					{
						using var fileStream = file.OpenReadStream();
						var fileName = $"/{Guid.NewGuid() + extension}";
						var uploadResult = await dropboxClient.Files.UploadAsync(
							path: fileName,
							WriteMode.Overwrite.Instance,
							body: fileStream);

						var sharedLink = await dropboxClient.Sharing.CreateSharedLinkWithSettingsAsync(uploadResult.PathLower);
						string directLink = sharedLink.Url.Replace("&dl=0", "&raw=1");
						newImageLinks.Add(directLink);
					}
				}

				category.ImgUrl = newImageLinks;
			}
		
			if (!string.IsNullOrWhiteSpace(form.Name))
			{
				category.Name = form.Name;
			}
			if (!string.IsNullOrWhiteSpace(form.Description))
			{
				category.Description = form.Description;
			}
			if (!string.IsNullOrWhiteSpace(form.Slug))
			{
				category.Slug = form.Slug;
			}
			category.CreatedAt ??= DateTime.Now;
			
			try
			{
				await container.ReplaceItemAsync(category, categoryId.ToString(), new PartitionKey("categories"));
				restResponse.status = new Status { code = 0, message = "Категория успешно обновлена" };
				restResponse.data = category;
			}
			catch (CosmosException ex)
			{
				restResponse.status = new Status { code = 500, message = $"Ошибка при обновлении категории: {ex.Message}" };
			}

			return restResponse;
		}

		[HttpPost("deletecategory")]
		public async Task<RestResponse> DeleteCategory([FromHeader(Name = "Authorization")] string token, [FromBody] Guid categoryId)
		{
			RestResponse restResponse = new()
			{
				meta = new()
				{
					{ "endpoint", "api/prod/deletecategory" },
					{ "time", DateTime.Now.Ticks },
				}
			};

			if (string.IsNullOrEmpty(token))
			{
				restResponse.status = new Status { code = 8, message = "Пустой токен" };
				restResponse.data = token;
				return restResponse;
			}

			if (!await _tokenVerificationService.TokenIsValid(token))
			{
				restResponse.status = new Status { code = 8, message = "Неверный или отсутствует токен" };
				restResponse.data = token;
				return restResponse;
			}

			var container = await _containerProvider.GetContainerAsync();

			try
			{
				// Получаем категорию по ID
				ItemResponse<Category> response = await container.ReadItemAsync<Category>(categoryId.ToString(), new PartitionKey("categories"));
				Category category = response.Resource;

				// Проверяем, что категория не была удалена ранее
				if (category.DeletedAt != null)
				{
					restResponse.status = new Status { code = 11, message = "Категория уже была удалена" };
					return restResponse;
				}

				// Устанавливаем время удаления
				category.DeletedAt = DateTime.Now;

				// Обновляем категорию
				await container.ReplaceItemAsync(category, categoryId.ToString(), new PartitionKey("categories"));

				restResponse.status = new Status { code = 0, message = "Категория успешно удалена (установлено время удаления)" };
				restResponse.data = category;
			}
			catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
			{
				restResponse.status = new Status { code = 404, message = "Категория не найдена" };
			}
			catch (CosmosException ex)
			{
				restResponse.status = new Status { code = 500, message = $"Ошибка при удалении категории: {ex.Message}" };
			}

			return restResponse;
		}
	}
}