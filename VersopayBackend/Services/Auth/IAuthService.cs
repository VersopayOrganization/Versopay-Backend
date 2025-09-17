using VersopayBackend.Dtos;
using static VersopayBackend.Dtos.PasswordResetDtos;

namespace VersopayBackend.Services.Auth
{
    public sealed record AuthResult(AuthResponseDto Response, string RefreshRaw, DateTime RefreshExpiresUtc);

    public interface IAuthService
    {
        Task<LoginOutcomeDto> LoginOrChallengeAsync(LoginDto loginDto, string? ip, string? userAgent, string? bypassRaw, CancellationToken ct);
        Task<AuthResult?> RefreshAsync(string rawRefresh, string? ip, string? userAgent, CancellationToken cancellationToken);
        Task LogoutAsync(string? rawRefresh, CancellationToken cancellationToken);

        Task ResetSenhaRequestAsync(SenhaEsquecidaRequest senhaEsquecidaDto, string baseResetUrl, string? ip, string? ua, CancellationToken cancellationToken);
        Task<bool> ValidarTokenResetSenhaAsync(string rawToken, CancellationToken cancellationToken);
        Task<bool> ResetSenhaAsync(RedefinirSenhaRequest redefinirSenhaDto, CancellationToken cancellationToken);

        Task<DeviceTrustChallengeDto> StartDeviceTrustAsync(int usuarioId, string? ip, string? ua, CancellationToken cancellationToken);
        Task<(string Raw, DateTime Exp)?> ConfirmDeviceTrustAsync(Guid challengeId, string code, string? ip, string? ua, CancellationToken cancellationToken);
        (string Raw, DateTime Exp)? ConsumePendingBypassCookie();
        Task<AuthWithPanelsResult?> ConfirmDeviceTrustAndIssueTokensAsync(
                        Guid challengeId, string code, string? ip, string? ua, CancellationToken ct);

        Task SendWelcomeEmail(string email, string nome, CancellationToken ct);
    }
}