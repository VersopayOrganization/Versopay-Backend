using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public interface ITransferenciaMatchRepository
    {
        Task<Transferencia?> GetByGatewayIdAsync(string gatewayId, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }

}
