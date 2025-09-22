using VersopayLibrary.Models;

namespace VersopayBackend.Repositories.Webhook
{
    public interface IInboundWebhookLogRepository
    {
        Task<bool> ExistsByEventKeyAsync(string eventKey, CancellationToken cancellationToken);
        Task AddAsync(InboundWebhookLog log, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
