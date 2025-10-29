using VersopayLibrary.Enums;

namespace VersopayBackend.Dtos
{
    public sealed class ProviderCredentialResponseDto
    {
        public int Id { get; set; }
        public PaymentProvider Provider { get; set; }
        public DateTime CriadoEmUtc { get; set; }
        public DateTime? AtualizadoEmUtc { get; set; }
    }
}
