using System.ComponentModel.DataAnnotations;

namespace VersopayBackend.Dtos
{
    public sealed record DeviceTrustChallengeDto(Guid ChallengeId, DateTime ExpiresAtUtc, string MaskedEmail);
    public sealed class DeviceTrustConfirmRequest
    {
        [Required]
        public Guid ChallengeId { get; set; }

        [Required, MaxLength(12)]
        public string Code { get; set; } = default!;
    }
}
