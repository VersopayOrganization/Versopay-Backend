using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VersopayBackend.Dtos;
using VersopayBackend.Services.Auth;
using static VersopayBackend.Dtos.PasswordResetDtos;

namespace VersopayBackend.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(IAuthService auth) : ControllerBase
    {
        const string RefreshCookieName = "rtkn";
        const string BypassCookieName = "bptkn";

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            // lê cookie do device (trusted device / bypass)
            var bypassRaw = Request.Cookies[BypassCookieName];

            var result = await auth.LoginAsync(
                loginDto,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                bypassRaw,                    
                cancellationToken
            );

            if (result is null) return Unauthorized(new { message = "Credenciais inválidas." });

            SetRefreshCookie(result.RefreshRaw, result.RefreshExpiresUtc);

            // Se o service gerou um novo bypass, seta o cookie do device
            if (auth is AuthService concrete)
            {
                var pending = concrete.ConsumePendingBypassCookie();
                if (pending is not null)
                {
                    SetBypassCookie(pending.Value.Raw, pending.Value.Exp);
                }
            }

            return Ok(result.Response);
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Refresh(CancellationToken cancellationToken)
        {
            var raw = Request.Cookies[RefreshCookieName];
            if (string.IsNullOrWhiteSpace(raw)) return Unauthorized();

            var result = await auth.RefreshAsync(raw,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(), cancellationToken);

            if (result is null) return Unauthorized();

            SetRefreshCookie(result.RefreshRaw, result.RefreshExpiresUtc);
            return Ok(result.Response);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            await auth.LogoutAsync(Request.Cookies[RefreshCookieName], cancellationToken);
            Response.Cookies.Delete(RefreshCookieName, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });
            // opcional: também remover bypass ao sair deste dispositivo
            // Response.Cookies.Delete(BypassCookieName, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });
            return NoContent();
        }

        [HttpPost("esqueci-senha")]
        [AllowAnonymous]
        public async Task<IActionResult> EsqueciSenha([FromBody] SenhaEsquecidaRequest senhaEsquecidaRequest, CancellationToken cancellationToken)
        {
            await auth.ResetSenhaRequestAsync(
                senhaEsquecidaRequest,
                baseResetUrl: string.Empty,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                cancellationToken);
            return NoContent();
        }

        [HttpGet("resetar-senha/validar")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidarResetToken([FromQuery] string token, CancellationToken cancellationToken)
        {
            var ok = await auth.ValidarTokenResetSenhaAsync(token, cancellationToken);
            return ok ? Ok() : BadRequest(new { message = "Token inválido ou expirado." });
        }

        [HttpPost("resetar-senha")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetarSenha([FromBody] RedefinirSenhaRequest redefinirSenhaRequest, CancellationToken cancellationToken)
        {
            var ok = await auth.ResetSenhaAsync(redefinirSenhaRequest, cancellationToken);
            return ok ? NoContent() : BadRequest(new { message = "Token inválido/expirado ou senhas não conferem." });
        }

        [Authorize]
        [HttpPost("device/start")]
        public async Task<ActionResult<DeviceTrustChallengeDto>> StartDeviceTrust(CancellationToken ct)
        {
            var userId = int.Parse(User.FindFirst("sub")!.Value);
            var dto = await auth.StartDeviceTrustAsync(userId,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(), ct);
            return Ok(dto);
        }

        [Authorize]
        [HttpPost("device/confirm")]
        public async Task<IActionResult> ConfirmDeviceTrust([FromBody] DeviceTrustConfirmRequest req, CancellationToken ct)
        {
            var result = await auth.ConfirmDeviceTrustAsync(req.ChallengeId, req.Code,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(), ct);

            if (result is null) return BadRequest(new { message = "Código inválido ou challenge expirado." });

            // setar o cookie bptkn
            Response.Cookies.Append("bptkn", result.Value.Raw, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = result.Value.Exp
            });
            return NoContent();
        }

        private void SetRefreshCookie(string rawToken, DateTime expiresUtc)
        {
            Response.Cookies.Append(RefreshCookieName, rawToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = expiresUtc
            });
        }

        private void SetBypassCookie(string rawToken, DateTime expiresUtc)
        {
            Response.Cookies.Append(BypassCookieName, rawToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = expiresUtc
            });
        }
    }
}
