using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public interface IInboundWebhookLogRepository
    {
        Task<bool> ExistsByEventKeyAsync(string eventKey, CancellationToken cancellationToken);
        Task AddAsync(InboundWebhookLog log, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
