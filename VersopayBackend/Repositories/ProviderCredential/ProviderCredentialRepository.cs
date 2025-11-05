using Microsoft.EntityFrameworkCore;
using VersopayBackend.Options;
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

                // >>> Importante para VexyBank:
                existing.ApiKey = cred.ApiKey;
                existing.ApiSecret = cred.ApiSecret;
                existing.WebhookSignatureSecret = cred.WebhookSignatureSecret;

                // token cache
                existing.AccessToken = cred.AccessToken;
                existing.AccessTokenExpiresUtc = cred.AccessTokenExpiresUtc;

                existing.AtualizadoEmUtc = DateTime.UtcNow;
            }
        }

        public async Task<List<string>> GetAllVexyWebhookSecretsAsync(CancellationToken ct) =>
            await db.Set<ProviderCredential>()
                    .Where(c => c.Provider == PaymentProvider.Vexy && c.WebhookSignatureSecret != null && c.WebhookSignatureSecret != "")
                    .Select(c => c.WebhookSignatureSecret!)
                    .ToListAsync(ct);

        // >>> NOVO:
        public async Task<string?> GetVexyWebhookSecretByOwnerAsync(int ownerUserId, CancellationToken ct)
        {
            var cred = await db.Set<ProviderCredential>()
                               .AsNoTracking()
                               .FirstOrDefaultAsync(c => c.OwnerUserId == ownerUserId
                                                      && c.Provider == PaymentProvider.Vexy, ct);
            return cred?.WebhookSignatureSecret;
        }

        public async Task RotateVexyWebhookSecretAsync(int ownerUserId, string? newSecret, CancellationToken ct)
        {
            var cred = await GetAsync(ownerUserId, PaymentProvider.Vexy, ct)
                ?? throw new InvalidOperationException("Credenciais Vexy não configuradas.");

            // Se newSecret == null, gere aqui:
            newSecret ??= WebhookSecretGenerator.NewHexSecret();

            // Move o atual para "previous", grava o novo como "current"
            cred.PrevWebhookSignatureSecret = cred.WebhookSignatureSecret;
            cred.WebhookSignatureSecret = newSecret;
            cred.AtualizadoEmUtc = DateTime.UtcNow;
            await SaveChangesAsync(ct);
        }

        public async Task<(string? current, string? previous)> GetVexySecretsPairAsync(int ownerUserId, CancellationToken ct)
        {
            var cred = await GetAsync(ownerUserId, PaymentProvider.Vexy, ct);
            return (cred?.WebhookSignatureSecret, cred?.PrevWebhookSignatureSecret);
        }


        public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    }
}
