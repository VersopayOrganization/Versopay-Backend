using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories.Webhook
{
    public sealed class PedidoMatchRepository(AppDbContext appDbContext) : IPedidoMatchRepository
    {
        public Task<Pedido?> GetByGatewayIdAsync(string gatewayId, CancellationToken cancellationToken) =>
            appDbContext.Pedidos.FirstOrDefaultAsync(pedido => pedido.GatewayTransactionId == gatewayId, cancellationToken);

        public Task<Pedido?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken) =>
            appDbContext.Pedidos.FirstOrDefaultAsync(pedido => pedido.ExternalId == externalId, cancellationToken);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => appDbContext.SaveChangesAsync(cancellationToken);
    }
}
