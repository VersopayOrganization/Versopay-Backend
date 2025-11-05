using VersopayLibrary.Enums;
using VersopayLibrary.Models; 

namespace VersopayBackend.Repositories
{
    // leitura agregada usada por FaturamentoService
    public interface IPedidoReadRepository
    {
        Task<(int qtd, decimal total)> GetVendasAprovadasAsync(int vendedorId, DateTime? dataInicio, DateTime? dataFim, CancellationToken ct);
        Task<MetodoStatsRaw> GetStatsPorMetodoAsync(int vendedorId, MetodoPagamento metodo, DateTime? dataInicio, DateTime? dataFim, CancellationToken ct);
        Task<(int qtd, decimal total)> GetChargebackAsync(int vendedorId, DateTime? dataInicio, DateTime? dataFim, CancellationToken ct);

        Task<List<Pedido>> GetAllAsync(
            StatusPedido? status,
            int? vendedorId,
            MetodoPagamento? metodo,
            DateTime? dataInicio,
            DateTime? dataFim,
            int page,
            int pageSize,
            CancellationToken ct);
    }

    public sealed class MetodoStatsRaw
    {
        public int QtdTotal { get; set; }
        public decimal Total { get; set; }
        public int QtdAprovado { get; set; }
        public decimal TotalAprovado { get; set; }
    }
}
