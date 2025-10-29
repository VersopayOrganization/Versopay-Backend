// VersopayBackend.Repositories.IWebhookRepository
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public interface IWebhookRepository
    {
        IQueryable<Webhook> QueryNoTracking();

        Task<Webhook?> FindByIdAsync(int id, CancellationToken ct);

        Task<Webhook?> GetByIdNoTrackingAsync(int id, CancellationToken ct);

        Task AddAsync(Webhook entity, CancellationToken ct);

        Task RemoveAsync(Webhook entity, CancellationToken ct);

        Task SaveChangesAsync(CancellationToken ct);
    }
}
