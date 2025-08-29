using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VersopayBackend.Dtos;
using VersopayBackend.Services.Auth;

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
