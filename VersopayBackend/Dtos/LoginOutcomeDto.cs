namespace VersopayBackend.Dtos
{
    public sealed class LoginOutcomeDto
    {
        public bool ChallengeRequired { get; init; }
        public DeviceTrustChallengeDto? Challenge { get; init; }   // quando 2FA for exigido
        public AuthResponseDto? Auth { get; init; }                // quando login concluído
        public string? RefreshRaw { get; init; }
        public DateTime? RefreshExpiresUtc { get; init; }
    }
}
