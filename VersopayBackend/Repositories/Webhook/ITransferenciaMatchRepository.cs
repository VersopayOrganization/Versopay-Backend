using VersopayLibrary.Models;

namespace VersopayBackend.Repositories.Webhook
{
    public interface ITransferenciaMatchRepository
    {
        Task<Transferencia?> GetByGatewayIdAsync(string gatewayId, CancellationToken cancellationToken);
        Task<Transferencia?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
