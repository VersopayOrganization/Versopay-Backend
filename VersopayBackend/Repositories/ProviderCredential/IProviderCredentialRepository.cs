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
        // PADRONIZE AQUI PARA List<string>
        // NOVO (apenas 1 segredo do owner):

        // ✅ NOVO (genérico por provider) — usado pelo VexyVendorWebhookHandler:
        Task<string?> GetWebhookSecretAsync(int ownerUserId, PaymentProvider provider, CancellationToken ct);
        // (opcional) par atual/anterior para rotação genérica:
        Task<(string? current, string? previous)> GetWebhookSecretsPairAsync(int ownerUserId, PaymentProvider provider, CancellationToken ct);
        Task<string?> GetVexyWebhookSecretByOwnerAsync(int ownerUserId, CancellationToken ct);
        Task RotateVexyWebhookSecretAsync(int ownerUserId, string? newSecret, CancellationToken ct);
        Task<(string? current, string? previous)> GetVexySecretsPairAsync(int ownerUserId, CancellationToken ct);

    }

}
