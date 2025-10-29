using System.Security.Claims;
using VersopayBackend.Dtos;
using VersopayLibrary.Enums;

namespace VersopayBackend.Services.Provider
{
    public interface IProviderCredentialsService
    {
        Task<ProviderCredentialResponseDto> UpsertAsync(ClaimsPrincipal user, ProviderCredentialUpsertDto dto, CancellationToken ct);
        Task<ProviderCredentialResponseDto?> GetMineAsync(ClaimsPrincipal user, PaymentProvider provider, CancellationToken ct);

    }
}
