using VersopayBackend.Dtos;

namespace VersopayBackend.Services.Auth
{
    public sealed record AuthResult(AuthResponseDto Response, string RefreshRaw, DateTime RefreshExpiresUtc);

    public interface IAuthService
    {
        Task<AuthResult?> LoginAsync(LoginDto dto, string? ip, string? userAgent, CancellationToken ct);
        Task<AuthResult?> RefreshAsync(string rawRefresh, string? ip, string? userAgent, CancellationToken ct);
        Task LogoutAsync(string? rawRefresh, CancellationToken ct);
    }
}