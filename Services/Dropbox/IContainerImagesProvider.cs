using Dropbox.Api;
using Dropbox.Api.Files;

namespace shnurok.Services.Dropbox
{
	public interface IContainerImagesProvider
	{
		Task<DropboxClient> GetDropboxClientContainerAsync();
	}
}
