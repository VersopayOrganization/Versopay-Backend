using VersopayLibrary.Models;

namespace VersopayBackend.Auth
{
    public interface ITokenService
    {
        string CreateToken(Usuario u, DateTime nowUtc, out DateTime expiresAtUtc);
    }
}
