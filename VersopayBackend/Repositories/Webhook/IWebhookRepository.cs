using VersopayLibrary.Models;

using webhook = VersopayLibrary.Models.Webhook;

namespace VersopayBackend.Repositories.Webhook
{
    public interface IWebhookRepository
    {
        IQueryable<webhook> QueryNoTracking();
        Task<webhook?> FindByIdAsync(int id, CancellationToken cancellationToken);
        Task<webhook?> GetByIdNoTrackingAsync(int id, CancellationToken cancellationToken);
        Task AddAsync(webhook webHook, CancellationToken cancellationToken);
        Task RemoveAsync(webhook webHook, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
