using System.Security.Cryptography;
using System.Text;

namespace VersopayBackend.Auth
{
    public class RefreshTokenService : IRefreshTokenService
    {
        public (string rawToken, string tokenHash, DateTime expiresUtc) Create(TimeSpan lifetime)
        {
            // 32 bytes aleatórios -> base64url
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            var raw = Convert.ToBase64String(bytes)
                .TrimEnd('=').Replace('+', '-').Replace('/', '_'); // url-safe
            var hash = Hash(raw);
            return (raw, hash, DateTime.UtcNow.Add(lifetime));
        }

        public string Hash(string rawToken)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToHexString(bytes); // 64 chars
        }
    }
}
