using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public interface ITransferenciaRepository
    {
        IQueryable<Transferencia> QueryNoTracking();

        Task AddAsync(Transferencia transferencia, CancellationToken cancellationToken);
        Task<Transferencia?> FindByIdAsync(int id, CancellationToken cancellationToken);
        Task<Transferencia?> GetByIdNoTrackingAsync(int id, CancellationToken cancellationToken);

        Task<List<Transferencia>> GetAllAsync(
            int? solicitanteId,
            StatusTransferencia? status,
            DateTime? dataInicio,
            DateTime? dataFim,
            int page,
            int pageSize,
            CancellationToken cancellationToken);

        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
