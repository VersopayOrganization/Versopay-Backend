using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories.VexyClient.PixIn
{
    public sealed class VexyBankPixInRepository(AppDbContext db) : IVexyBankPixInRepository
    {
        public Task AddAsync(VexyBankPixIn e, CancellationToken ct)
            => db.VexyBankPixIns.AddAsync(e, ct).AsTask();

        public Task<VexyBankPixIn?> FindByExternalIdAsync(int ownerUserId, string externalId, CancellationToken ct)
            => db.VexyBankPixIns.FirstOrDefaultAsync(
                x => x.OwnerUserId == ownerUserId && x.ExternalId == externalId, ct);

        public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    }
}
