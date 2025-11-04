using VersopayLibrary.Enums;

namespace VersopayBackend.Repositories
{
    public interface IProviderCredentialRepository
    {
        Task<ProviderCredential?> GetAsync(int ownerUserId, PaymentProvider provider, CancellationToken ct);
        Task<ProviderCredential?> FindByClientAsync(PaymentProvider provider, string clientId, string clientSecret, CancellationToken ct);
        Task AddOrUpdateAsync(ProviderCredential cred, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
        Task<List<string>> GetAllVexyWebhookSecretsAsync(CancellationToken ct);

    }

}
