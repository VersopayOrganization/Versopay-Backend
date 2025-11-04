using System.Text.Json.Serialization;

namespace VersopayBackend.Dtos.VexyBank
{
    public sealed class PixOutDataDto
    {
        [JsonPropertyName("id")] public string Id { get; set; } = default!;
        [JsonPropertyName("status")] public string Status { get; set; } = default!;
        [JsonPropertyName("amount")] public int Amount { get; set; }
        [JsonPropertyName("netAmount")] public int NetAmount { get; set; }
        [JsonPropertyName("fees")] public int Fees { get; set; }
        [JsonPropertyName("pixKey")] public string PixKey { get; set; } = default!;
    }
}
