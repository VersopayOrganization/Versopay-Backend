using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;
using VersopayLibrary.Enums;

namespace VersopayBackend.Repositories
{
    public sealed class ProviderCredentialRepository(AppDbContext db) : IProviderCredentialRepository
    {
        public Task<ProviderCredential?> GetAsync(int ownerUserId, PaymentProvider provider, CancellationToken ct) =>
            db.ProviderCredentials.FirstOrDefaultAsync(x => x.OwnerUserId == ownerUserId && x.Provider == provider, ct);

        public Task<ProviderCredential?> FindByClientAsync(PaymentProvider provider, string clientId, string clientSecret, CancellationToken ct)
        {
            clientId = clientId.Trim();
            clientSecret = clientSecret.Trim();
            return db.ProviderCredentials
                     .FirstOrDefaultAsync(x => x.Provider == provider && x.ClientId == clientId && x.ClientSecret == clientSecret, ct);
        }

        public async Task AddOrUpdateAsync(ProviderCredential cred, CancellationToken ct)
        {
            var existing = await GetAsync(cred.OwnerUserId, cred.Provider, ct);
            if (existing is null)
            {
                await db.ProviderCredentials.AddAsync(cred, ct);
            }
            else
            {
                existing.ClientId = cred.ClientId;
                existing.ClientSecret = cred.ClientSecret;
                existing.AccessToken = cred.AccessToken;
                existing.AccessTokenExpiresUtc = cred.AccessTokenExpiresUtc;
                existing.AtualizadoEmUtc = DateTime.UtcNow;
            }
        }

        public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    }

}
