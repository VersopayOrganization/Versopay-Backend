using VersopayLibrary.Models;

namespace VersopayBackend.Repositories.Webhook
{
    public interface IPedidoMatchRepository
    {
        Task<Pedido?> GetByGatewayIdAsync(string gatewayId, CancellationToken cancellationToken);
        Task<Pedido?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
