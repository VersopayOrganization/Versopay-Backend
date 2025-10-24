using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public sealed class PedidoRepository(AppDbContext appDbContext) : IPedidoRepository, IPedidoReadRepository
    {
        public Task AddAsync(Pedido pedido, CancellationToken cancellationToken) =>
            appDbContext.Pedidos.AddAsync(pedido, cancellationToken).AsTask();

        public Task<Pedido?> FindByIdAsync(int id, CancellationToken cancellationToken) =>
            appDbContext.Pedidos.FirstOrDefaultAsync(pedido => pedido.Id == id, cancellationToken);

        public Task<Pedido?> GetByIdNoTrackingAsync(int id, CancellationToken cancellationToken) =>
            appDbContext.Pedidos.AsNoTracking()
                      .Include(pedido => pedido.Vendedor)
                      .FirstOrDefaultAsync(pedido => pedido.Id == id, cancellationToken);

        private static IQueryable<Pedido> BaseQ(AppDbContext appDbContext, int vendedorId, DateTime? dataInicio, DateTime? dataFim)
        {
            var query = appDbContext.Pedidos.AsNoTracking().Where(pedido => pedido.VendedorId == vendedorId);
            if (dataInicio.HasValue) query = query.Where(pedido => pedido.Criacao >= dataInicio.Value);
            if (dataFim.HasValue) query = query.Where(pedido => pedido.Criacao < dataFim.Value);
            return query;
        }

        public async Task<(int qtd, decimal total)> GetVendasAprovadasAsync(int vendedorId, DateTime? dataInicio, DateTime? dataFim, CancellationToken cancellationToken)
        {
            var query = BaseQ(appDbContext, vendedorId, dataInicio, dataFim).Where(pedido => pedido.Status == StatusPedido.Aprovado);
            var quantidade = await query.CountAsync(cancellationToken);
            var total = await query.SumAsync(pedido => (decimal?)pedido.Valor, cancellationToken) ?? 0m;
            return (quantidade, total);
        }

        public async Task<MetodoStatsRaw> GetStatsPorMetodoAsync(int vendedorId, MetodoPagamento metodo, DateTime? dataInicio, DateTime? dataFim, CancellationToken cancellationToken)
        {
            var query = BaseQ(appDbContext, vendedorId, dataInicio, dataFim).Where(pedido => pedido.MetodoPagamento == metodo);

            var totalQtd = await query.CountAsync(cancellationToken);
            var totalVal = await query.SumAsync(pedido => (decimal?)pedido.Valor, cancellationToken) ?? 0m;

            var quantidadeAprovados = query.Where(pedido => pedido.Status == StatusPedido.Aprovado);
            var quantidadeAprovadosQtd = await quantidadeAprovados.CountAsync(cancellationToken);
            var quantidadeAprovadosValor = await quantidadeAprovados.SumAsync(p => (decimal?)p.Valor, cancellationToken) ?? 0m;

            return new MetodoStatsRaw { QtdTotal = totalQtd, Total = totalVal, QtdAprovado = quantidadeAprovadosQtd, TotalAprovado = quantidadeAprovadosValor };
        }

        public async Task<(int qtd, decimal total)> GetChargebackAsync(int vendedorId, DateTime? dataInicio, DateTime? dataFim, CancellationToken cancellationToken)
        {
            // ajuste o status conforme seu enum (Estornado/Chargeback)
            var query = BaseQ(appDbContext, vendedorId, dataInicio, dataFim).Where(pedido => pedido.Status == StatusPedido.Estornado);
            var quantidade = await query.CountAsync(cancellationToken);
            var total = await query.SumAsync(pedido => (decimal?)pedido.Valor, cancellationToken) ?? 0m;
            return (quantidade, total);
        }

        public async Task<List<Pedido>> GetAllAsync(
            StatusPedido? status,
            int? vendedorId,
            MetodoPagamento? metodo,
            DateTime? dataInicio,
            DateTime? dataFim,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 200) pageSize = 20;

            var query = appDbContext.Pedidos.AsNoTracking()
                              .Include(pedido => pedido.Vendedor)
                              .AsQueryable();

            if (status.HasValue) query = query.Where(pedido => pedido.Status == status.Value);
            if (vendedorId.HasValue) query = query.Where(pedido => pedido.VendedorId == vendedorId.Value);
            if (metodo.HasValue) query = query.Where(pedido => pedido.MetodoPagamento == metodo.Value);
            if (dataInicio.HasValue) query = query.Where(pedido => pedido.Criacao >= dataInicio.Value);
            if (dataFim.HasValue) query = query.Where(pedido => pedido.Criacao < dataFim.Value);

            return await query.OrderByDescending(pedido => pedido.Criacao)
                          .Skip((page - 1) * pageSize)
                          .Take(pageSize)
                          .ToListAsync(cancellationToken);
        }

        public async Task<int> GetCountAllAsync(
            StatusPedido? status,
            int? vendedorId,
            MetodoPagamento? metodo,
            DateTime? dataInicio,
            DateTime? dataFim,
            CancellationToken cancellationToken)
        {

            var query = appDbContext.Pedidos.AsNoTracking()
                              .Include(pedido => pedido.Vendedor)
                              .AsQueryable();

            if (status.HasValue) query = query.Where(pedido => pedido.Status == status.Value);
            if (vendedorId.HasValue) query = query.Where(pedido => pedido.VendedorId == vendedorId.Value);
            if (metodo.HasValue) query = query.Where(pedido => pedido.MetodoPagamento == metodo.Value);
            if (dataInicio.HasValue) query = query.Where(pedido => pedido.Criacao >= dataInicio.Value);
            if (dataFim.HasValue) query = query.Where(pedido => pedido.Criacao < dataFim.Value);

            return await query.CountAsync(cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => appDbContext.SaveChangesAsync(cancellationToken);
    }
}
