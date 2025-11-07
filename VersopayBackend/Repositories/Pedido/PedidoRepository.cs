using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public sealed class PedidoRepository : IPedidoRepository, IPedidoReadRepository
    {
        private readonly AppDbContext _db;
        public PedidoRepository(AppDbContext db) => _db = db;

        public Task AddAsync(Pedido entity, CancellationToken ct) =>
            _db.Pedidos.AddAsync(entity, ct).AsTask();

        public Task<Pedido?> FindByIdAsync(int id, CancellationToken ct) =>
            _db.Pedidos.FirstOrDefaultAsync(p => p.Id == id, ct);

        public Task<Pedido?> GetByIdNoTrackingAsync(int id, CancellationToken ct) =>
            _db.Pedidos.AsNoTracking()
               .Include(p => p.Vendedor)
               .FirstOrDefaultAsync(p => p.Id == id, ct);

        private IQueryable<Pedido> BaseQ(int vendedorId, DateTime? de, DateTime? ate)
        {
            var q = _db.Pedidos.AsNoTracking().Where(p => p.VendedorId == vendedorId);
            if (de.HasValue) q = q.Where(p => p.Criacao >= de.Value);
            if (ate.HasValue) q = q.Where(p => p.Criacao < ate.Value);
            return q;
        }

        public async Task<(int qtd, decimal total)> GetVendasAprovadasAsync(int vendedorId, DateTime? de, DateTime? ate, CancellationToken ct)
        {
            var q = BaseQ(vendedorId, de, ate).Where(p => p.Status == StatusPedido.Aprovado);
            var qtd = await q.CountAsync(ct);
            var total = await q.SumAsync(p => (decimal?)p.Valor, ct) ?? 0m;
            return (qtd, total);
        }

        public async Task<MetodoStatsRaw> GetStatsPorMetodoAsync(int vendedorId, MetodoPagamento metodo, DateTime? de, DateTime? ate, CancellationToken ct)
        {
            var q = BaseQ(vendedorId, de, ate).Where(p => p.MetodoPagamento == metodo);

            var totalQtd = await q.CountAsync(ct);
            var totalVal = await q.SumAsync(p => (decimal?)p.Valor, ct) ?? 0m;

            var aprov = q.Where(p => p.Status == StatusPedido.Aprovado);
            var aprovQtd = await aprov.CountAsync(ct);
            var aprovVal = await aprov.SumAsync(p => (decimal?)p.Valor, ct) ?? 0m;

            return new MetodoStatsRaw { QtdTotal = totalQtd, Total = totalVal, QtdAprovado = aprovQtd, TotalAprovado = aprovVal };
        }

        public async Task<(int qtd, decimal total)> GetChargebackAsync(int vendedorId, DateTime? de, DateTime? ate, CancellationToken ct)
        {
            var q = BaseQ(vendedorId, de, ate).Where(p => p.Status == StatusPedido.Estornado);
            var qtd = await q.CountAsync(ct);
            var total = await q.SumAsync(p => (decimal?)p.Valor, ct) ?? 0m;
            return (qtd, total);
        }

        public async Task<List<Pedido>> GetAllAsync(
            StatusPedido? status,
            int? vendedorId,
            MetodoPagamento? metodo,
            DateTime? dataInicio,
            DateTime? dataFim,
            int page,
            int pageSize,
            CancellationToken ct)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 200) pageSize = 20;

            var q = _db.Pedidos.AsNoTracking()
                               .Include(p => p.Vendedor)
                               .AsQueryable();

            if (status.HasValue) q = q.Where(p => p.Status == status.Value);
            if (vendedorId.HasValue) q = q.Where(p => p.VendedorId == vendedorId.Value);
            if (metodo.HasValue) q = q.Where(p => p.MetodoPagamento == metodo.Value);
            if (dataInicio.HasValue) q = q.Where(p => p.Criacao >= dataInicio.Value);
            if (dataFim.HasValue) q = q.Where(p => p.Criacao < dataFim.Value);

            return await q.OrderByDescending(p => p.Criacao)
                          .Skip((page - 1) * pageSize)
                          .Take(pageSize)
                          .ToListAsync(ct);
        }

        public async Task<int> GetCountAllAsync(
            StatusPedido? status,
            int? vendedorId,
            MetodoPagamento? metodo,
            DateTime? dataInicio,
            DateTime? dataFim,
            CancellationToken ct)
        {
            var q = _db.Pedidos.AsNoTracking().AsQueryable();

            if (status.HasValue) q = q.Where(p => p.Status == status.Value);
            if (vendedorId.HasValue) q = q.Where(p => p.VendedorId == vendedorId.Value);
            if (metodo.HasValue) q = q.Where(p => p.MetodoPagamento == metodo.Value);
            if (dataInicio.HasValue) q = q.Where(p => p.Criacao >= dataInicio.Value);
            if (dataFim.HasValue) q = q.Where(p => p.Criacao < dataFim.Value);

            return await q.CountAsync(ct);
        }

        public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
    }
}
