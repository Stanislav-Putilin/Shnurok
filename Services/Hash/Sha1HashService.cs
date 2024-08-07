namespace shnurok.Services.Hash
{
    public class Sha1HashService : IHashService
    {
        public String Digest(String input)
        {
            return Convert.ToHexString(
                System.Security.Cryptography.SHA1.HashData(
                    System.Text.Encoding.UTF8.GetBytes(input)
            ));
        }
    }
}
