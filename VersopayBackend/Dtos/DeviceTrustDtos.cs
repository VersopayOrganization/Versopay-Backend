using System.ComponentModel.DataAnnotations;

namespace VersopayBackend.Dtos
{
    public sealed record DeviceTrustChallengeDto(Guid ChallengeId, DateTime ExpiresAtUtc, string MaskedEmail);
    public sealed record DeviceTrustConfirmRequest([property: Required] Guid ChallengeId, [property: Required, MaxLength(12)] string Code);
}
