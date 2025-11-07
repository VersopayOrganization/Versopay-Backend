using System.Text.Json.Serialization;

namespace VersopayBackend.Dtos.VexyBank
{
    public sealed class VexyWebhookEnvelope
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }            // "transaction" | "transfer"
        [JsonPropertyName("event")] public string? Event { get; set; }          // ex.: "transaction_paid"
        [JsonPropertyName("scope")] public string? Scope { get; set; }          // ex.: "postback"

        [JsonPropertyName("transaction")] public VexyTransactionDto? Transaction { get; set; }
        [JsonPropertyName("transfer")] public VexyTransferDto? Transfer { get; set; }
    }
}
