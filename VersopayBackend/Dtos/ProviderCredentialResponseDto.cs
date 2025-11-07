using System.Text.Json.Serialization;
using VersopayLibrary.Enums;

namespace VersopayBackend.Dtos
{
    public sealed class ProviderCredentialResponseDto
    {
        public int Id { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PaymentProvider Provider { get; set; }
        public DateTime CriadoEmUtc { get; set; }
        public DateTime? AtualizadoEmUtc { get; set; }
    }
}
