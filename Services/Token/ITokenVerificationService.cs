namespace shnurok.Services.Token
{
	public interface ITokenVerificationService
	{
		Task<bool> TokenIsValid(string token);
	}
}
