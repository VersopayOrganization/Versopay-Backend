using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var result = await auth.LoginAsync(loginDto,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(), cancellationToken);

            if (result is null) return Unauthorized(new { message = "Credenciais inválidas." });

            SetRefreshCookie(result.RefreshRaw, result.RefreshExpiresUtc);
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
            return NoContent();
        }

        [HttpPost("esqueci-senha")]
        [AllowAnonymous]
        public async Task<IActionResult> EsqueciSenha([FromBody] SenhaEsquecidaRequest senhaEsquecidaRequest, CancellationToken cancellationToken)
        {
            var baseResetUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host + "/reset";

            var link = await auth.ResetSenhaRequestAsync(
                senhaEsquecidaRequest,
                baseResetUrl,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                cancellationToken);

            //TODO: QUANDO TIVERMOS O ENVIO POR EMAIL, NAO PODEMOS RETORNAR O LINK NO ENDPOINT! Deverá ser descomentado essa linha de baixo
            //return NoContent();

            //E removido essa linha
            return Ok(new { resetLink = link });

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
    }
}