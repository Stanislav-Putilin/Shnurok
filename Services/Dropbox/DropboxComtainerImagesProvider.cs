using Dropbox.Api;
using Dropbox.Api.Files;

namespace shnurok.Services.Dropbox
{
	public class DropboxContainerImagesProvider : IContainerImagesProvider
	{
		private readonly IConfiguration _configuration;
		private DropboxClient _dropboxClient = null!;

		public DropboxContainerImagesProvider(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		public async Task<DropboxClient> GetDropboxClientContainerAsync()
		{
			if (_dropboxClient == null)
			{
				try
				{
					var dropboxSection = _configuration.GetSection("Dropbox");
					var tokenSection = dropboxSection?.GetSection("DropboxToken");
					var dropboxToken = tokenSection?.Value;
					
					if (string.IsNullOrEmpty(dropboxToken))
					{
						throw new InvalidOperationException("Один или несколько параметров dropbox конфигурации отсутствуют.");
					}

					_dropboxClient = new DropboxClient(dropboxToken);					
				}				
				catch (Exception ex)
				{
					Console.WriteLine($"Неизвестная ошибка: {ex.Message}");
				}
			}

			return _dropboxClient;
		}
	}
}
