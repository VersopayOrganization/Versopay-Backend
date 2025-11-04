using System.Text.Json.Serialization;

namespace VersopayBackend.Dtos.VexyBank
{
    public sealed class PixItemDto
    {
        [JsonPropertyName("title")] public string Title { get; set; } = default!;
        [JsonPropertyName("tangible")] public bool Tangible { get; set; }
        [JsonPropertyName("quantity")] public int Quantity { get; set; }
        [JsonPropertyName("amountInCents")] public int AmountInCents { get; set; }
    }
}
