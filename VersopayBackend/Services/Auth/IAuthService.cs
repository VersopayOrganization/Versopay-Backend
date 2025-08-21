using VersopayBackend.Dtos;
using static VersopayBackend.Dtos.PasswordResetDtos;

namespace VersopayBackend.Services.Auth
{
    public sealed record AuthResult(AuthResponseDto Response, string RefreshRaw, DateTime RefreshExpiresUtc);

    public interface IAuthService
    {
        Task<AuthResult?> LoginAsync(LoginDto loginDto, string? ip, string? userAgent, CancellationToken cancellationToken);
        Task<AuthResult?> RefreshAsync(string rawRefresh, string? ip, string? userAgent, CancellationToken cancellationToken);
        Task LogoutAsync(string? rawRefresh, CancellationToken cancellationToken);
        Task ResetSenhaRequestAsync(SenhaEsquecidaRequest dto, string baseResetUrl, string? ip, string? ua, CancellationToken cancellationToken);
        Task<bool> ValidarTokenResetSenhaAsync(string rawToken, CancellationToken cancellationToken);
        Task<bool> ResetSenhaAsync(RedefinirSenhaRequest dto, CancellationToken cancellationToken);
    }
}