using System.Security.Cryptography;
using System.Text;

namespace VersopayBackend.Utils
{
    public static class CryptoUtils
    {
        // ---------------------------
        // SHA-256
        // ---------------------------

        public static string Sha256Base64(string input)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            var bytes = Encoding.UTF8.GetBytes(input);
            return Sha256Base64(bytes);
        }

        public static string Sha256Base64(byte[] data)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(data);
            return Convert.ToBase64String(hash);
        }

        public static string Sha256Hex(string input, bool upper = false)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            var bytes = Encoding.UTF8.GetBytes(input);
            return Sha256Hex(bytes, upper);
        }

        public static string Sha256Hex(byte[] data, bool upper = false)
        {
            if (data is null) throw new ArgumentNullException(nameof(data));
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(data);
            return ToHex(hash, upper);
        }

        // ---------------------------
        // HMAC-SHA256
        // ---------------------------

        public static string HmacSha256Base64(string key, string message)
        {
            if (key is null) throw new ArgumentNullException(nameof(key));
            if (message is null) throw new ArgumentNullException(nameof(message));
            return HmacSha256Base64(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(message));
        }

        public static string HmacSha256Base64(byte[] key, byte[] message)
        {
            if (key is null) throw new ArgumentNullException(nameof(key));
            if (message is null) throw new ArgumentNullException(nameof(message));

            using var hmac = new HMACSHA256(key);
            var mac = hmac.ComputeHash(message);
            return Convert.ToBase64String(mac);
        }

        public static string HmacSha256Hex(string key, string message, bool upper = false)
        {
            if (key is null) throw new ArgumentNullException(nameof(key));
            if (message is null) throw new ArgumentNullException(nameof(message));
            return HmacSha256Hex(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(message), upper);
        }

        public static string HmacSha256Hex(byte[] key, byte[] message, bool upper = false)
        {
            if (key is null) throw new ArgumentNullException(nameof(key));
            if (message is null) throw new ArgumentNullException(nameof(message));

            using var hmac = new HMACSHA256(key);
            var mac = hmac.ComputeHash(message);
            return ToHex(mac, upper);
        }

        // ---------------------------
        // Constant-time comparison
        // ---------------------------

        /// <summary>
        /// Compara duas strings (ex.: assinaturas) em tempo constante para evitar timing attacks.
        /// Trata null como string vazia para evitar NRE e manter semântica estável.
        /// </summary>
        public static bool ConstantTimeEquals(string? a, string? b)
        {
            // Normaliza null -> "" para comparar de forma estável
            var aBytes = Encoding.UTF8.GetBytes(a ?? string.Empty);
            var bBytes = Encoding.UTF8.GetBytes(b ?? string.Empty);
            return ConstantTimeEquals(aBytes, bBytes);
        }

        public static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            // Não lance exceção por null; trate como arrays vazios
            a ??= Array.Empty<byte>();
            b ??= Array.Empty<byte>();

            // Diferenças de tamanho contam no diff, mas não saem cedo
            int diff = a.Length ^ b.Length;
            int len = Math.Min(a.Length, b.Length);

            for (int i = 0; i < len; i++)
            {
                diff |= a[i] ^ b[i];
            }

            // Se os tamanhos diferem, já está refletido em 'diff'
            return diff == 0;
        }

        // ---------------------------
        // Helpers
        // ---------------------------

        private static string ToHex(byte[] bytes, bool upper)
        {
            var c = upper ? "X2" : "x2";
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString(c));
            return sb.ToString();
        }
    }
}
