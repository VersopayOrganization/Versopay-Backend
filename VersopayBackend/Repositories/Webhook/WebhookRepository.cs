// VersopayBackend.Repositories.WebhookRepository
using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;

namespace VersopayBackend.Repositories
{
    public sealed class WebhookRepository(AppDbContext db) : IWebhookRepository
    {
        public IQueryable<VersopayLibrary.Models.Webhook> QueryNoTracking()
            => db.Webhooks.AsNoTracking();

        public Task<VersopayLibrary.Models.Webhook?> FindByIdAsync(int id, CancellationToken ct)
            => db.Webhooks.FirstOrDefaultAsync(x => x.Id == id, ct);

        public Task<VersopayLibrary.Models.Webhook?> GetByIdNoTrackingAsync(int id, CancellationToken ct)
            => db.Webhooks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

        public Task AddAsync(VersopayLibrary.Models.Webhook entity, CancellationToken ct)
            => db.Webhooks.AddAsync(entity, ct).AsTask();

        public Task RemoveAsync(VersopayLibrary.Models.Webhook entity, CancellationToken ct)
        {
            db.Webhooks.Remove(entity);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct)
            => db.SaveChangesAsync(ct);
    }
}
