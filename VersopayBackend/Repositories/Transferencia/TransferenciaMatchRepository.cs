using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public sealed class TransferenciaMatchRepository : ITransferenciaMatchRepository
    {
        private readonly AppDbContext _db;
        public TransferenciaMatchRepository(AppDbContext db) => _db = db;

        public Task<Transferencia?> GetByGatewayIdAsync(string gatewayId, CancellationToken ct) =>
            _db.Transferencias.FirstOrDefaultAsync(t => t.GatewayTransactionId == gatewayId, ct);

        public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
    }
}
