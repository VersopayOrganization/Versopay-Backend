using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public interface IUsuarioRepository
    {
        Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct);
        Task<RefreshToken?> GetRefreshWithUserByHashAsync(string tokenHash, CancellationToken ct);
        Task AddRefreshAsync(RefreshToken token, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}