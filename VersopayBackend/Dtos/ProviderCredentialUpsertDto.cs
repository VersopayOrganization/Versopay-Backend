using System.ComponentModel.DataAnnotations;
using VersopayLibrary.Enums;

namespace VersopayBackend.Dtos
{
    public sealed class ProviderCredentialUpsertDto
    {
        [Required] public PaymentProvider Provider { get; set; } // Vexy ou Versell
        [Required, MaxLength(512)] public string ClientId { get; set; } = default!;
        [Required, MaxLength(600)] public string ClientSecret { get; set; } = default!;

        // CAMPO OPCIONAL – se quiser guardar explicitamente as chaves Vexy
        [MaxLength(200)]
        public string? ApiKey { get; set; }

        [MaxLength(512)]
        public string? ApiSecret { get; set; }

        [MaxLength(256)]
        public string? WebhookSignatureSecret { get; set; }
    }
}
