namespace shnurok.Services.Kdf
{
    public interface IKdfService
    {
        String DerivedKey(String password, String? salt = null);
    }
}
