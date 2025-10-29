using System.Security.Claims;
using VersopayBackend.Dtos;                 // ProviderCredentialUpsertDto, ProviderCredentialResponseDto
using VersopayBackend.Repositories;        // IProviderCredentialRepository
using VersopayLibrary.Enums;               // PaymentProvider
using VersopayLibrary.Models;              // ProviderCredential

namespace VersopayBackend.Services.Provider
{

    public sealed class ProviderCredentialsService(IProviderCredentialRepository repo) : IProviderCredentialsService
    {
        private static int CurrentUserId(ClaimsPrincipal user)
        {
            var sub = user.FindFirst("sub")?.Value;
            if (string.IsNullOrWhiteSpace(sub))
                throw new UnauthorizedAccessException("Token sem 'sub'.");
            return int.Parse(sub);
        }

        public async Task<ProviderCredentialResponseDto> UpsertAsync(ClaimsPrincipal user, ProviderCredentialUpsertDto dto, CancellationToken ct)
        {
            var ownerId = CurrentUserId(user);

            // Tenta buscar existente
            var existing = await repo.GetAsync(ownerId, dto.Provider, ct);

            var entity = existing ?? new ProviderCredential
            {
                OwnerUserId = ownerId,
                Provider = dto.Provider,
                CriadoEmUtc = DateTime.UtcNow
            };

            // Atualiza credenciais
            entity.ClientId = dto.ClientId.Trim();
            entity.ClientSecret = dto.ClientSecret.Trim();
            entity.AtualizadoEmUtc = DateTime.UtcNow;

            // Se trocar client/secret, invalide token de sessão (Vexy)
            entity.AccessToken = null;
            entity.AccessTokenExpiresUtc = null;

            await repo.AddOrUpdateAsync(entity, ct);
            await repo.SaveChangesAsync(ct);

            return Map(entity);
        }

        public async Task<ProviderCredentialResponseDto?> GetMineAsync(ClaimsPrincipal user, PaymentProvider provider, CancellationToken ct)
        {
            var ownerId = CurrentUserId(user);
            var entity = await repo.GetAsync(ownerId, provider, ct);
            return entity is null ? null : Map(entity);
        }

        private static ProviderCredentialResponseDto Map(ProviderCredential e) => new()
        {
            Id = e.Id,
            Provider = e.Provider,
            CriadoEmUtc = e.CriadoEmUtc,
            AtualizadoEmUtc = e.AtualizadoEmUtc
            // Intencionalmente não retornamos ClientId/ClientSecret por segurança
        };
    }
}
