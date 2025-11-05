using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public interface IPedidoRepository
    {
        Task AddAsync(Pedido entity, CancellationToken ct);
        Task<Pedido?> FindByIdAsync(int id, CancellationToken ct);
        Task<Pedido?> GetByIdNoTrackingAsync(int id, CancellationToken ct);

        Task<List<Pedido>> GetAllAsync(
            StatusPedido? status,
            int? vendedorId,
            MetodoPagamento? metodo,
            DateTime? dataInicio,
            DateTime? dataFim,
            int page,
            int pageSize,
            CancellationToken ct);

        Task<int> GetCountAllAsync(
            StatusPedido? status,
            int? vendedorId,
            MetodoPagamento? metodo,
            DateTime? dataInicio,
            DateTime? dataFim,
            CancellationToken ct);

        Task SaveChangesAsync(CancellationToken ct);
    }
}
