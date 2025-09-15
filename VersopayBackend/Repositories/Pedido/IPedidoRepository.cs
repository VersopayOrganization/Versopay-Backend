using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public interface IPedidoRepository
    {
        Task AddAsync(Pedido pedido, CancellationToken cancellationToken);
        Task<Pedido?> FindByIdAsync(int id, CancellationToken cancellationToken);
        Task<Pedido?> GetByIdNoTrackingAsync(int id, CancellationToken cancellationToken);
        Task<List<Pedido>> GetAllAsync(
            StatusPedido? status,
            int? vendedorId,
            MetodoPagamento? metodo,
            DateTime? dataInicioUtc,
            DateTime? dataFimUtc,
            int page,
            int pageSize,
            CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
