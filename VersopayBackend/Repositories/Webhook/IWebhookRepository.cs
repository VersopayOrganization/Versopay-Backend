using VersopayLibrary.Models;

using webhook = VersopayLibrary.Models.Webhook;

namespace VersopayBackend.Repositories.Webhook
{
    public interface IWebhookRepository
    {
        IQueryable<webhook> QueryNoTracking();
        Task<webhook?> FindByIdAsync(int id, CancellationToken ct);
        Task<webhook?> GetByIdNoTrackingAsync(int id, CancellationToken ct);
        Task AddAsync(webhook entity, CancellationToken ct);
        Task RemoveAsync(webhook entity, CancellationToken ct); // se quiser hard delete
        Task SaveChangesAsync(CancellationToken ct);
    }
}
