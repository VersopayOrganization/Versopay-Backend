using System.Text.Json.Serialization;

namespace VersopayBackend.Dtos
{
    public sealed class VexyDepositReqDto
    {
        // A doc mostra number (100.00). Use decimal para não perder precisão.
        [JsonPropertyName("amount")]
        public decimal amount { get; set; }

        [JsonPropertyName("external_id")]
        public string external_id { get; set; } = default!;

        // A Vexy pede "clientCallbackUrl" (camelCase), conforme doc que você colou.
        [JsonPropertyName("clientCallbackUrl")]
        public string clientCallbackUrl { get; set; } = default!;

        [JsonPropertyName("payer")]
        public VexyPayerDto payer { get; set; } = new();
    }
}
