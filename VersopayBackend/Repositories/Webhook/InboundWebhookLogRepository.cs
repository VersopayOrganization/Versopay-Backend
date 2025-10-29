using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public sealed class InboundWebhookLogRepository(AppDbContext db) : IInboundWebhookLogRepository
    {
        public Task<bool> ExistsByEventKeyAsync(string eventKey, CancellationToken ct) =>
            db.Set<InboundWebhookLog>().AnyAsync(x => x.EventKey == eventKey, ct);

        public Task AddAsync(InboundWebhookLog log, CancellationToken ct) =>
            db.Set<InboundWebhookLog>().AddAsync(log, ct).AsTask();

        public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    }
}
