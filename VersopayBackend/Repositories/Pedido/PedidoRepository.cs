using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public sealed class PedidoRepository(AppDbContext appDbContext) : IPedidoRepository
    {
        public Task AddAsync(Pedido pedido, CancellationToken cancellationToken) =>
            appDbContext.Pedidos.AddAsync(pedido, cancellationToken).AsTask();

        public Task<Pedido?> FindByIdAsync(int id, CancellationToken cancellationToken) =>
            appDbContext.Pedidos.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        public Task<Pedido?> GetByIdNoTrackingAsync(int id, CancellationToken cancellationToken) =>
            appDbContext.Pedidos.AsNoTracking()
                      .Include(pedido => pedido.Vendedor)
                      .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        public async Task<List<Pedido>> GetAllAsync(
            StatusPedido? status,
            int? vendedorId,
            MetodoPagamento? metodo,
            DateTime? dataDeUtc,
            DateTime? dataAteUtc,
            int page,
            int pageSize,
            CancellationToken ct)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 200) pageSize = 20;

            var query = appDbContext.Pedidos.AsNoTracking()
                              .Include(pedido => pedido.Vendedor)
                              .AsQueryable();

            if (status.HasValue) query = query.Where(pedido => pedido.Status == status.Value);
            if (vendedorId.HasValue) query = query.Where(pedido => pedido.VendedorId == vendedorId.Value);
            if (metodo.HasValue) query = query.Where(pedido => pedido.MetodoPagamento == metodo.Value);
            if (dataDeUtc.HasValue) query = query.Where(pedido => pedido.Criacao >= dataDeUtc.Value);
            if (dataAteUtc.HasValue) query = query.Where(pedido => pedido.Criacao < dataAteUtc.Value);

            return await query.OrderByDescending(pedido => pedido.Criacao)
                          .Skip((page - 1) * pageSize)
                          .Take(pageSize)
                          .ToListAsync(ct);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => appDbContext.SaveChangesAsync(cancellationToken);
    }
}
