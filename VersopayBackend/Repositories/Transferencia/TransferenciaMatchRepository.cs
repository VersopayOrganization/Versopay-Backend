using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public sealed class TransferenciaMatchRepository(AppDbContext db) : ITransferenciaMatchRepository
    {
        public Task<Transferencia?> GetByGatewayIdAsync(string gatewayId, CancellationToken ct) =>
            db.Transferencias.FirstOrDefaultAsync(t => t.GatewayTransactionId == gatewayId, ct);

        public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    }
}
