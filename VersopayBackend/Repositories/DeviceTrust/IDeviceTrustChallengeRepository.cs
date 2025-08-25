using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public interface IDeviceTrustChallengeRepository
    {
        Task AddAsync(DeviceTrustChallenge entity, CancellationToken ct);
        Task<DeviceTrustChallenge?> GetAsync(Guid id, CancellationToken ct);
        Task InvalidateUserOpenAsync(int usuarioId, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}
