using System.Text.Json.Serialization;

namespace VersopayBackend.Dtos
{
    // Envelope genérico da Vexy (transaction/transfer + event)
    public sealed class VexyWebhookEnvelope
    {
        [JsonPropertyName("id")] public string Id { get; set; } = default!;
        [JsonPropertyName("type")] public string Type { get; set; } = default!;   // "transaction" | "transfer"
        [JsonPropertyName("event")] public string Event { get; set; } = default!;  // ex: "transaction_paid", "transfer_completed"
        [JsonPropertyName("scope")] public string? Scope { get; set; }             // "postback"
        [JsonPropertyName("transaction")] public VexyWebhookTransaction? Transaction { get; set; }
        [JsonPropertyName("transfer")] public VexyWebhookTransfer? Transfer { get; set; }
    }
}
