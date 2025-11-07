using System.Text.Json.Serialization;

namespace VersopayBackend.Dtos.VexyBank
{
    public sealed class VexyPayerInfoDto
    {
        [JsonPropertyName("bank")] public string? Bank { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("document")] public string? Document { get; set; }
    }
}
