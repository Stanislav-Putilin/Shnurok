namespace shnurok.Services.Token
{
    public interface ITokenService
    {
        Task<string> CreateTokenAsync(Guid userId);
    }
}
