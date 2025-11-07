using System.Text.Json.Serialization;

namespace VersopayBackend.Dtos.VexyBank
{
    public sealed class PixCustomerDto
    {
        [JsonPropertyName("name")] public string Name { get; set; } = default!;
        [JsonPropertyName("email")] public string Email { get; set; } = default!;
        [JsonPropertyName("documentType")] public string DocumentType { get; set; } = "cpf"; // cpf|cnpj
        [JsonPropertyName("document")] public string Document { get; set; } = default!;
        [JsonPropertyName("phone")] public string? Phone { get; set; }
    }
}
