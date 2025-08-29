using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public interface IBypassTokenRepository
    {
        Task<BypassToken?> GetByHashWithUserAsync(string tokenHash, CancellationToken ct);
        Task AddAsync(BypassToken entity, CancellationToken ct);
        Task RevokeAllByUserAsync(int usuarioId, DateTime whenUtc, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}
