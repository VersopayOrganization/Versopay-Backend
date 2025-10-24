using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories.Webhook
{
    public sealed class InboundWebhookLogRepository(AppDbContext appDbContext) : IInboundWebhookLogRepository
    {
        public Task<bool> ExistsByEventKeyAsync(string eventKey, CancellationToken cancellationToken) =>
            appDbContext.Set<InboundWebhookLog>().AnyAsync(inboundWebhookLog => inboundWebhookLog.EventKey == eventKey, cancellationToken);

        public Task AddAsync(InboundWebhookLog log, CancellationToken cancellationToken) =>
            appDbContext.Set<InboundWebhookLog>().AddAsync(log, cancellationToken).AsTask();

        public Task SaveChangesAsync(CancellationToken cancellationToken) => appDbContext.SaveChangesAsync(cancellationToken);
    }
}
