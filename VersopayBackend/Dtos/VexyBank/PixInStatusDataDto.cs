using System.Text.Json.Serialization;

namespace VersopayBackend.Dtos.VexyBank
{
    public sealed class PixInStatusDataDto
    {
        [JsonPropertyName("id")] public string Id { get; set; } = default!;
        [JsonPropertyName("status")] public string Status { get; set; } = default!; // pending|paid|canceled|refunded
        [JsonPropertyName("amount")] public int? Amount { get; set; }
        [JsonPropertyName("fees")] public int? Fees { get; set; }

        // Algumas implantações também retornam o nó "pix" no status — deixe opcional:
        [JsonPropertyName("pix")] public PixQrDto? Pix { get; set; }
    }
}
