using System.Text.Json.Serialization;

namespace VersopayBackend.Dtos
{
    public sealed class VexyDepositReqDto
    {
        public decimal amount { get; set; }
        public string external_id { get; set; } = default!;
        [JsonPropertyName("url_callback")]
        public string clientCallbackUrl { get; set; } = default!;  // será enviado como "url_callback"
        public VexyPayerDto payer { get; set; } = new();
    }
}
