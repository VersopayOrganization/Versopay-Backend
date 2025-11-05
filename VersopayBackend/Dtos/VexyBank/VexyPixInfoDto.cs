using System.Text.Json.Serialization;

namespace VersopayBackend.Dtos.VexyBank
{
    public sealed class VexyPixInfoDto
    {
        [JsonPropertyName("endToEndId")] public string? EndToEndId { get; set; }
        [JsonPropertyName("payerInfo")] public VexyPayerInfoDto? PayerInfo { get; set; }
    }
}
