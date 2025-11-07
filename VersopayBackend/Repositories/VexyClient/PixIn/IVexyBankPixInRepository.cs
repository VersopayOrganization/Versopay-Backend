using VersopayLibrary.Models;

namespace VersopayBackend.Repositories.VexyClient.PixIn
{
    public interface IVexyBankPixInRepository
    {
        Task AddAsync(VexyBankPixIn e, CancellationToken ct);
        Task<VexyBankPixIn?> FindByExternalIdAsync(int ownerUserId, string externalId, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}
