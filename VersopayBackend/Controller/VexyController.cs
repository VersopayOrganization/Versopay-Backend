// Controllers/VexyController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using VersopayBackend.Dtos;
using VersopayBackend.Services;

[ApiController]
[Route("api/vexy")]
[Authorize]
public class VexyController(IVexyService service) : ControllerBase
{
    private static int CurrentUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? throw new UnauthorizedAccessException("Token sem sub/nameid.");
        if (!int.TryParse(sub, out var id)) throw new UnauthorizedAccessException("sub inválido.");
        return id;
    }

    [HttpPost("validate")]
    public async Task<IActionResult> Validate(CancellationToken ct)
    {
        var (ok, error) = await service.ValidateCredentialsAsync(CurrentUserId(User), ct);
        return ok ? Ok(new { ok = true }) : BadRequest(new { ok = false, error });
    }

    [HttpPost("deposit")]
    public Task<VexyDepositRespDto> Deposit([FromBody] VexyDepositReqDto req, CancellationToken ct)
        => service.CreateDepositAsync(CurrentUserId(User), req, ct);

    [HttpPost("withdraw")]
    public Task<VexyWithdrawRespDto> Withdraw([FromBody] VexyWithdrawReqDto req, CancellationToken ct)
        => service.RequestWithdrawalAsync(CurrentUserId(User), req, ct);
}
