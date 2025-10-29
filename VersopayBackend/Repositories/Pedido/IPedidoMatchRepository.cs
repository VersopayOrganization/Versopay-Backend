using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public interface IPedidoMatchRepository
    {
        Task<Pedido?> GetByGatewayIdAsync(string gatewayId, CancellationToken ct);
        Task<Pedido?> GetByExternalIdAsync(string externalId, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}
