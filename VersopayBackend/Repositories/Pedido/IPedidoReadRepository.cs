using VersopayLibrary.Enums;
using VersopayLibrary.Models; 

namespace VersopayBackend.Repositories
{
    public interface IPedidoReadRepository
    {
        Task<(int qtd, decimal total)> GetVendasAprovadasAsync(int vendedorId, DateTime? de, DateTime? ate, CancellationToken ct);
        Task<MetodoStatsRaw> GetStatsPorMetodoAsync(int vendedorId, MetodoPagamento metodo, DateTime? de, DateTime? ate, CancellationToken ct);
        Task<(int qtd, decimal total)> GetChargebackAsync(int vendedorId, DateTime? de, DateTime? ate, CancellationToken ct);

        Task<List<Pedido>> GetAllAsync(
            StatusPedido? status,
            int? vendedorId,
            MetodoPagamento? metodo,
            DateTime? dataDeUtc,
            DateTime? dataAteUtc,
            int page,
            int pageSize,
            CancellationToken cancellationToken);
    }

    public sealed class MetodoStatsRaw
    {
        public int QtdTotal { get; set; }
        public decimal Total { get; set; }
        public int QtdAprovado { get; set; }
        public decimal TotalAprovado { get; set; }
    }
}
