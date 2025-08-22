
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public interface IKycKybRepository
    {
        Task AdicionarAsync(KycKyb item, CancellationToken cancellationToken);
        Task<KycKyb?> AcharPeloIdAsync(int id, CancellationToken cancellationToken);
        Task<KycKyb?> PegarPeloIdNoTrackingAsync(int id, CancellationToken cancellationToken);
        Task<List<KycKyb>> PegarTodosAsync(
            int? usuarioId,
            StatusKycKyb? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
