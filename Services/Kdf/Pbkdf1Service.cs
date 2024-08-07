using shnurok.Services.Hash;

namespace shnurok.Services.Kdf
{
    public class Pbkdf1Service : IKdfService
    {
        private readonly IHashService _hashService;

        public Pbkdf1Service(IHashService hashService)
        {
            _hashService = hashService;
        }

        public string DerivedKey(string password, string? salt = null)
        {
            salt ??= _hashService.Digest(Guid.NewGuid().ToString())[..20];
            String t1 = _hashService.Digest(password + salt);
            t1 = _hashService.Digest(t1);
            return $"{salt}.{t1[..20]}";
        }
    }
}
