using System.Text.Json.Serialization;

namespace VersopayBackend.Dtos.VexyBank
{
    public sealed class PixInDataDto
    {
        [JsonPropertyName("id")] public string Id { get; set; } = default!;
        [JsonPropertyName("status")] public string Status { get; set; } = "pending";
        [JsonPropertyName("fees")] public int Fees { get; set; }
        [JsonPropertyName("pix")] public PixQrDto Pix { get; set; } = new();
    }
}
