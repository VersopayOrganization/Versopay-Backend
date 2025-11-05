namespace VersopayBackend.Options
{
    public static class WebhookSecretGenerator
    {
        // 32 bytes -> 64 chars hex (ou use Base64Url se preferir)
        public static string NewHexSecret(int bytes = 32)
        {
            var data = System.Security.Cryptography.RandomNumberGenerator.GetBytes(bytes);
            return Convert.ToHexString(data).ToLowerInvariant();
        }
    }

}
