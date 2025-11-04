using System.Text.Json.Serialization;

namespace VersopayBackend.Dtos.VexyBank
{
    public sealed class PixOutReqDto
    {
        [JsonPropertyName("pixKey")] public string PixKey { get; set; } = default!;
        [JsonPropertyName("amount")] public int Amount { get; set; }  // em centavos
        [JsonPropertyName("currency")] public string Currency { get; set; } = "BRL";
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("postbackUrl")] public string? PostbackUrl { get; set; }
    }
}
