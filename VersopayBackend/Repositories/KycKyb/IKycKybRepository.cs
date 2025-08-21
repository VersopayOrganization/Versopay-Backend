
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public interface IKycKybRepository
    {
        Task AddAsync(KycKyb item, CancellationToken ct);
        Task<KycKyb?> FindByIdAsync(int id, CancellationToken ct);
        Task<KycKyb?> GetByIdNoTrackingAsync(int id, CancellationToken ct);
        Task<List<KycKyb>> GetAllAsync(
            int? usuarioId,
            StatusKycKyb? status,
            int page,
            int pageSize,
            CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}
