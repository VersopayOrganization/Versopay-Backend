using System.Text.Json.Serialization;

namespace VersopayBackend.Dtos
{
    public sealed class VexyWebhookTransfer
    {
        [JsonPropertyName("id")] public string Id { get; set; } = default!;
        [JsonPropertyName("amount")] public int Amount { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; } = default!;
        [JsonPropertyName("pix")] public object? Pix { get; set; }
    }
}
