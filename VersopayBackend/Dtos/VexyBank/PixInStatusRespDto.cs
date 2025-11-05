using System.Text.Json.Serialization;

namespace VersopayBackend.Dtos.VexyBank
{
    public sealed class PixInStatusRespDto
    {
        [JsonPropertyName("success")] public bool Success { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
        [JsonPropertyName("data")] public PixInStatusDataDto Data { get; set; } = new();
    }
}
