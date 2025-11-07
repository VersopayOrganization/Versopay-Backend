using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using VersopayBackend.Dtos;
using VersopayBackend.Repositories;
using VersopayBackend.Services;
using VersopayBackend.Services.Vexy;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Controllers
{
    [ApiController]
    [Route("api/providers")]
    [Authorize]
    public class ProviderController(IProviderCredentialRepository repo, ILogger<ProviderController> logger) : ControllerBase
    {
        // --- helpers --------------------------------------------------------
        private static int CurrentUserId(ClaimsPrincipal user)
        {
            var id =
                user.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                user.FindFirstValue(ClaimTypes.NameIdentifier) ??
                user.FindFirstValue("uid");

            if (string.IsNullOrWhiteSpace(id))
                throw new UnauthorizedAccessException("Token sem identificador de usuário (sub/nameid/uid).");

            if (!int.TryParse(id, out var intId))
                throw new UnauthorizedAccessException("Identificador de usuário inválido no token (esperado int).");

            return intId;
        }

        private static ProviderCredentialResponseDto Map(ProviderCredential e) => new()
        {
            Id = e.Id,
            Provider = e.Provider,
            CriadoEmUtc = e.CriadoEmUtc,
            AtualizadoEmUtc = e.AtualizadoEmUtc
        };

        // --- endpoints ------------------------------------------------------

        [HttpPost("credentials")]
        public async Task<ActionResult<ProviderCredentialResponseDto>> Upsert(
            [FromBody] ProviderCredentialUpsertDto dto,
            CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var ownerId = CurrentUserId(User);

            var entity = await repo.GetAsync(ownerId, dto.Provider, ct);
            if (entity is null)
            {
                entity = new ProviderCredential
                {
                    OwnerUserId = ownerId,
                    Provider = dto.Provider,
                    ClientId = dto.ClientId.Trim(),
                    ClientSecret = dto.ClientSecret.Trim(),
                    ApiKey = string.IsNullOrWhiteSpace(dto.ApiKey) ? null : dto.ApiKey.Trim(),
                    ApiSecret = string.IsNullOrWhiteSpace(dto.ApiSecret) ? null : dto.ApiSecret.Trim(),
                    WebhookSignatureSecret = string.IsNullOrWhiteSpace(dto.WebhookSignatureSecret) ? null : dto.WebhookSignatureSecret.Trim(),
                    AccessToken = null,
                    AccessTokenExpiresUtc = null,
                    CriadoEmUtc = DateTime.UtcNow
                };
            }
            else
            {
                entity.ClientId = dto.ClientId.Trim();
                entity.ClientSecret = dto.ClientSecret.Trim();
                entity.ApiKey = string.IsNullOrWhiteSpace(dto.ApiKey) ? entity.ApiKey : dto.ApiKey.Trim();
                entity.ApiSecret = string.IsNullOrWhiteSpace(dto.ApiSecret) ? entity.ApiSecret : dto.ApiSecret.Trim();
                entity.WebhookSignatureSecret = string.IsNullOrWhiteSpace(dto.WebhookSignatureSecret) ? entity.WebhookSignatureSecret : dto.WebhookSignatureSecret.Trim();
                entity.AtualizadoEmUtc = DateTime.UtcNow;
                entity.AccessToken = null;
                entity.AccessTokenExpiresUtc = null;
            }

            await repo.AddOrUpdateAsync(entity, ct);
            await repo.SaveChangesAsync(ct);

            return Ok(Map(entity));
        }

        [HttpGet("credentials/{provider}")]
        public async Task<ActionResult<ProviderCredentialResponseDto>> GetOne(
            [FromRoute] PaymentProvider provider,
            CancellationToken ct)
        {
            var ownerId = CurrentUserId(User);
            var entity = await repo.GetAsync(ownerId, provider, ct);
            return entity is null ? NotFound() : Ok(Map(entity));
        }

        [HttpPost("credentials/validate")]
        public async Task<IActionResult> Validate(CancellationToken ct, [FromServices] IVexyService vexy)
        {
            var ownerId = CurrentUserId(User);
            var (ok, error) = await vexy.ValidateCredentialsAsync(ownerId, ct);
            if (ok) return Ok(new { ok = true, provider = "Vexy" });
            return BadRequest(new { ok = false, provider = "Vexy", error });
        }

        [HttpPost("credentials/validate-bank")]
        public async Task<IActionResult> ValidateBank(CancellationToken ct, [FromServices] IVexyBankService vexyBank)
        {
            var ownerId = CurrentUserId(User);
            var (ok, error) = await vexyBank.ValidateCredentialsAsync(ownerId, ct);
            if (ok) return Ok(new { ok = true, provider = "VexyBank" });
            return BadRequest(new { ok = false, provider = "VexyBank", error });
        }
    }
}
