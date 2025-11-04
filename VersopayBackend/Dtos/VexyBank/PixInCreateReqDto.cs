using System.Text.Json.Serialization;

namespace VersopayBackend.Dtos.VexyBank
{
    public sealed class PixInCreateReqDto
    {
        [JsonPropertyName("amountInCents")] public int AmountInCents { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("postbackUrl")] public string? PostbackUrl { get; set; }
        [JsonPropertyName("customer")] public PixCustomerDto Customer { get; set; } = new();
        [JsonPropertyName("items")] public List<PixItemDto>? Items { get; set; }
    }
}
