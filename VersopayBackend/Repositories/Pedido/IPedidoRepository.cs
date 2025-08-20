using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public interface IPedidoRepository
    {
        Task AddAsync(Pedido p, CancellationToken ct);
        Task<Pedido?> FindByIdAsync(int id, CancellationToken ct);
        Task<Pedido?> GetByIdNoTrackingAsync(int id, CancellationToken ct);
        Task<List<Pedido>> GetAllAsync(
            StatusPedido? status,
            int? vendedorId,
            MetodoPagamento? metodo,
            DateTime? dataDeUtc,
            DateTime? dataAteUtc,
            int page,
            int pageSize,
            CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}
