using VersopayLibrary.Enums;
using VersopayLibrary.Models; 

namespace VersopayBackend.Repositories
{
    public interface IPedidoReadRepository
    {
        Task<(int qtd, decimal total)> GetVendasAprovadasAsync(int vendedorId, DateTime? dataInicio, DateTime? dataFim, CancellationToken cancellationToken);
        Task<MetodoStatsRaw> GetStatsPorMetodoAsync(int vendedorId, MetodoPagamento metodo, DateTime? dataInicio, DateTime? dataFim, CancellationToken cancellationToken);
        Task<(int qtd, decimal total)> GetChargebackAsync(int vendedorId, DateTime? dataInicio, DateTime? dataFim, CancellationToken cancellationToken);

        Task<List<Pedido>> GetAllAsync(
            StatusPedido? status,
            int? vendedorId,
            MetodoPagamento? metodo,
            DateTime? dataInicio,
            DateTime? dataFim,
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
