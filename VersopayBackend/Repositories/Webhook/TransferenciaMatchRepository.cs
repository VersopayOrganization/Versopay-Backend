using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories.Webhook
{
    public sealed class TransferenciaMatchRepository(AppDbContext appDbContext) : ITransferenciaMatchRepository
    {
        public Task<Transferencia?> GetByGatewayIdAsync(string gatewayId, CancellationToken cancellationToken) =>
            appDbContext.Transferencias.FirstOrDefaultAsync(transferencia => transferencia.GatewayTransactionId == gatewayId, cancellationToken);

        public Task<Transferencia?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken) =>
            appDbContext.Transferencias.FirstOrDefaultAsync(transferencia => transferencia.ExternalId == externalId, cancellationToken);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => appDbContext.SaveChangesAsync(cancellationToken);
    }
}
