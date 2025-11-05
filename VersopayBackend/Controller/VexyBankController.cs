// Controllers/VexyBankController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using VersopayBackend.Dtos.VexyBank;
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

    [HttpPost("pix-out")]
    public Task<PixOutRespDto> PixOut([FromBody] PixOutReqDto req,
        [FromHeader(Name = "X-Idempotency-Key")] string idempotencyKey,
        CancellationToken ct)
        => service.SendPixOutAsync(CurrentUserId(User), req, idempotencyKey, ct);
}
