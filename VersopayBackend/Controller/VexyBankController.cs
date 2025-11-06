// Controllers/VexyBankController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using VersopayBackend.Dtos.VexyBank;
using VersopayBackend.Repositories.VexyClient.PixIn;
using VersopayBackend.Services.Vexy;

[ApiController]
[Route("api/vexybank")]
[Authorize]
public class VexyBankController(IVexyBankService service) : ControllerBase
{
    static int CurrentUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? throw new UnauthorizedAccessException("Token sem sub/nameid.");
        if (!int.TryParse(sub, out var id)) throw new UnauthorizedAccessException("sub inválido.");
        return id;
    }

    [HttpPost("pix-in/qrcode")]
    public Task<PixInCreateRespDto> CreateQrCode([FromBody] PixInCreateReqDto req, CancellationToken ct)
        => service.CreatePixInAsync(CurrentUserId(User), req, ct);

    // NOVO: consultar depósito por id
    [HttpGet("pix-in/{id}")]
    public Task<PixInStatusRespDto> GetPixIn([FromRoute] string id, CancellationToken ct)
        => service.GetPixInAsync(CurrentUserId(User), id, ct);

    [HttpGet("pix-in/local/{id}")]
    public async Task<ActionResult<object>> GetPixInLocal(
    [FromRoute] string id,
    [FromServices] IVexyBankPixInRepository repo,
    CancellationToken ct)
    {
        var ownerId = CurrentUserId(User);
        var e = await repo.FindByExternalIdAsync(ownerId, id, ct);
        if (e is null) return NotFound();

        return Ok(new
        {
            e.ExternalId,
            e.Status,
            e.AmountCents,
            e.PixEmv,
            HasQr = !string.IsNullOrEmpty(e.QrPngBase64),
            e.PostbackUrl,
            e.PayerDocument,
            e.CreatedAtUtc,
            e.UpdatedAtUtc,
            e.PaidAtUtc
        });
    }

    [HttpPost("pix-out")]
    public Task<PixOutRespDto> PixOut([FromBody] PixOutReqDto req,
        [FromHeader(Name = "X-Idempotency-Key")] string idempotencyKey,
        CancellationToken ct)
        => service.SendPixOutAsync(CurrentUserId(User), req, idempotencyKey, ct);
}
