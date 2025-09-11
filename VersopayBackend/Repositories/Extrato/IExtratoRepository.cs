using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public interface IExtratoRepository
    {
        Task<Extrato?> GetByClienteIdAsync(int clienteId, CancellationToken cancellationToken);
        Task<Extrato?> GetByClienteIdNoTrackingAsync(int clienteId, CancellationToken cancellationToken);
        Task AddAsync(Extrato extrato, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
