using System.Text.Json.Serialization;

namespace VersopayBackend.Dtos.VexyBank
{
    public sealed class VexyTransactionDto
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("amount")] public int Amount { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }        // "pending" | "paid" | ...
        [JsonPropertyName("pix")] public VexyPixInfoDto? Pix { get; set; }
        [JsonPropertyName("customer")] public VexyCustomerDto? Customer { get; set; }
    }
}
