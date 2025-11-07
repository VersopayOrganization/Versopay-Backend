using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public interface IExtratoRepository
    {
        Task<Extrato?> GetByClienteIdAsync(int clienteId, CancellationToken ct);
        Task<Extrato?> GetByClienteIdNoTrackingAsync(int clienteId, CancellationToken ct);
        Task AddAsync(Extrato entity, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}
