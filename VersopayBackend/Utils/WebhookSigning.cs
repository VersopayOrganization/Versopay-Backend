using System.Security.Cryptography;
using System.Text;

namespace VersopayBackend.Utils
{
    public static class WebhookSigning
    {
        public static string SignBodySha256(string body, string secret)
        {
            using var h = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = h.ComputeHash(Encoding.UTF8.GetBytes(body));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
