using System.Text.Json.Serialization;

namespace VersopayBackend.Dtos
{
    public sealed class VexyPayerDto
    {
        [JsonPropertyName("name")]
        public string name { get; set; } = default!;

        [JsonPropertyName("email")]
        public string email { get; set; } = default!;

        // Somente dígitos
        [JsonPropertyName("document")]
        public string document { get; set; } = default!;

        // A doc não exige explicitamente, mas manteremos opcional
        [JsonPropertyName("document_type")]
        public string? document_type { get; set; }

        [JsonPropertyName("phone")]
        public string? phone { get; set; }
    }
}
