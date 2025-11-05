using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public interface ITransferenciaRepository
    {
        IQueryable<Transferencia> QueryNoTracking();
        Task AddAsync(Transferencia entity, CancellationToken ct);
        Task<Transferencia?> FindByIdAsync(int id, CancellationToken ct);
        Task<Transferencia?> GetByIdNoTrackingAsync(int id, CancellationToken ct);
        Task<List<Transferencia>> GetAllAsync(
            int? solicitanteId,
            StatusTransferencia? status,
            DateTime? dataInicio,
            DateTime? dataFim,
            int page,
            int pageSize,
            CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}
