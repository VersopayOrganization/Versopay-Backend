using System.Text.Json.Serialization;

namespace VersopayBackend.Dtos.VexyBank
{
    public sealed class PixQrDto
    {
        [JsonPropertyName("emv")] public string Emv { get; set; } = default!;
        [JsonPropertyName("qrCode")] public string QrCodeBase64 { get; set; } = default!;
    }
}
