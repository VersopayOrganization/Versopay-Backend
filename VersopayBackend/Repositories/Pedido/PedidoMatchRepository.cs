using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public sealed class PedidoMatchRepository(AppDbContext db) : IPedidoMatchRepository
    {
        public Task<Pedido?> GetByGatewayIdAsync(string gatewayId, CancellationToken ct) =>
            db.Pedidos.FirstOrDefaultAsync(p => p.GatewayTransactionId == gatewayId, ct);

        public Task<Pedido?> GetByExternalIdAsync(string externalId, CancellationToken ct) =>
            db.Pedidos.FirstOrDefaultAsync(p => p.ExternalId == externalId, ct);

        public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    }
}
