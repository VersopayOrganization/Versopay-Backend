using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;
using VersopayLibrary.Models;

using webhook = VersopayLibrary.Models.Webhook;

namespace VersopayBackend.Repositories.Webhook
{
    public sealed class WebhookRepository(AppDbContext db) : IWebhookRepository
    {
        public IQueryable<webhook> QueryNoTracking() => db.Webhooks.AsNoTracking();

        public Task<webhook?> FindByIdAsync(int id, CancellationToken ct) =>
            db.Webhooks.FirstOrDefaultAsync(x => x.Id == id, ct);

        public Task<webhook?> GetByIdNoTrackingAsync(int id, CancellationToken ct) =>
            db.Webhooks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

        public Task AddAsync(webhook entity, CancellationToken ct) =>
            db.Webhooks.AddAsync(entity, ct).AsTask();

        public Task RemoveAsync(webhook entity, CancellationToken ct)
        {
            db.Webhooks.Remove(entity);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    }
}
