using System.ComponentModel.DataAnnotations;
using VersopayLibrary.Enums;

namespace VersopayBackend.Dtos
{
    public sealed class ProviderCredentialUpsertDto
    {
        [Required] public PaymentProvider Provider { get; set; } // Vexy ou Versell
        [Required, MaxLength(160)] public string ClientId { get; set; } = default!;
        [Required, MaxLength(160)] public string ClientSecret { get; set; } = default!;
    }
}
