// VersopayBackend/Utils/VexySignatureVerifier.cs
using System.Security.Cryptography;
using System.Text;

namespace VersopayBackend.Utils
{
    public static class VexySignatureVerifier
    {
        // Retorna true se a assinatura bater. Timestamp opcionalmente checado (tolerância em segundos).
        public static bool Verify(string? signatureHeader, string rawBody, string secret, int? timestampToleranceSeconds = 300)
        {
            if (string.IsNullOrWhiteSpace(signatureHeader)) return false;

            // Header é algo como: "t=1580306324381,v1=abcdef..."
            string? t = null;
            string? v1 = null;

            foreach (var part in signatureHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var kv = part.Split('=', 2);
                if (kv.Length != 2) continue;
                if (kv[0].Equals("t", StringComparison.OrdinalIgnoreCase)) t = kv[1];
                if (kv[0].Equals("v1", StringComparison.OrdinalIgnoreCase)) v1 = kv[1];
            }

            if (string.IsNullOrWhiteSpace(t) || string.IsNullOrWhiteSpace(v1)) return false;

            if (timestampToleranceSeconds.HasValue && long.TryParse(t, out var tMs))
            {
                var webhookTime = DateTimeOffset.FromUnixTimeMilliseconds(tMs).UtcDateTime;
                var now = DateTime.UtcNow;
                if (Math.Abs((now - webhookTime).TotalSeconds) > timestampToleranceSeconds.Value)
                    return false; // timestamp muito antigo/novo
            }

            var signedPayload = $"{t}.{rawBody}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
            var hex = Convert.ToHexString(hash).ToLowerInvariant();

            // comparação em tempo constante
            return FixedTimeEquals(hex, v1.ToLowerInvariant());
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            if (a.Length != b.Length) return false;
            var result = 0;
            for (int i = 0; i < a.Length; i++)
                result |= a[i] ^ b[i];
            return result == 0;
        }
    }
}
