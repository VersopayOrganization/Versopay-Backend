using VersopayLibrary.Enums;

namespace VersopayBackend.Repositories
{
    public interface IPedidoReadRepository
    {
        Task<(int qtd, decimal total)> GetVendasAprovadasAsync(int vendedorId, DateTime? de, DateTime? ate, CancellationToken ct);
        Task<MetodoStatsRaw> GetStatsPorMetodoAsync(int vendedorId, MetodoPagamento metodo, DateTime? de, DateTime? ate, CancellationToken ct);
        Task<(int qtd, decimal total)> GetChargebackAsync(int vendedorId, DateTime? de, DateTime? ate, CancellationToken ct);
    }

    public sealed class MetodoStatsRaw
    {
        public int QtdTotal { get; set; }
        public decimal Total { get; set; }
        public int QtdAprovado { get; set; }
        public decimal TotalAprovado { get; set; }
    }
}
