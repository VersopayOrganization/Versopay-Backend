using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VersopayBackend.Dtos;
using VersopayBackend.Services.Auth;

namespace VersopayBackend.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(IAuthService auth, IHostEnvironment env) : ControllerBase
    {
        const string RefreshCookieName = "rtkn";
        const string BypassCookieName = "bptkn";

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            try
            {
                var bypassRaw = Request.Cookies[BypassCookieName];
                var outcome = await auth.LoginOrChallengeAsync(
                    loginDto,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers.UserAgent.ToString(),
                    bypassRaw,
                    ct
                );

                if (outcome.Auth is null && !outcome.ChallengeRequired)
                    return Unauthorized(new { message = "Credenciais inválidas." });

                if (outcome.ChallengeRequired)
                    return Accepted(new { requires2fa = true, challenge = outcome.Challenge });

                if (string.IsNullOrWhiteSpace(outcome.RefreshRaw) || outcome.RefreshExpiresUtc is null)
                    return Problem("Falha ao emitir refresh token.", statusCode: 500);

                SetRefreshCookie(outcome.RefreshRaw, outcome.RefreshExpiresUtc.Value);

                var pending = auth.ConsumePendingBypassCookie();
                if (pending is not null)
                    SetBypassCookie(pending.Value.Raw, pending.Value.Exp);

                return Ok(outcome.Auth);
            }
            catch (InvalidOperationException ex)
            {
                // veio do StartDeviceTrustAsync (timeout/falha no e-mail)
                return Problem(ex.Message, statusCode: 500);
            }
        }

        /// <summary>
        /// Força o fluxo 2FA usando as credenciais: se NÃO houver device confiável, cria challenge e envia código.
        /// Se houver device confiável, já retorna tokens (mesma resposta do /login).
        /// </summary>
        [HttpPost("login/2fa/start")]
        [AllowAnonymous]
        public async Task<IActionResult> Login2FAStart([FromBody] LoginDto loginDto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            // Força revalidação sem depender do cookie bypass (bypassRaw = null)
            var outcome = await auth.LoginOrChallengeAsync(
                loginDto,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                bypassRaw: null,
                ct
            );

            if (outcome.Auth is null && !outcome.ChallengeRequired)
                return Unauthorized(new { message = "Credenciais inválidas." });

            if (outcome.ChallengeRequired)
                return Accepted(new { requires2fa = true, challenge = outcome.Challenge });

            SetRefreshCookie(outcome.RefreshRaw!, outcome.RefreshExpiresUtc!.Value);

            var pending = auth.ConsumePendingBypassCookie();
            if (pending is not null)
                SetBypassCookie(pending.Value.Raw, pending.Value.Exp);

            return Ok(outcome.Auth);
        }

        /// <summary>
        /// Confirma o código de 6 dígitos (2FA) e grava o cookie de device confiável (bypass).
        /// Observação: este endpoint NÃO emite access/refresh; após confirmar,
        /// o frontend deve chamar /api/auth/login novamente (com as mesmas credenciais) para receber os tokens.
        /// </summary>
        [HttpPost("login/2fa/confirm")]
        [AllowAnonymous]
        public async Task<IActionResult> Login2FAConfirm([FromBody] DeviceTrustConfirmRequest body, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var pair = await auth.ConfirmDeviceTrustAsync(
                body.ChallengeId,
                body.Code,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                ct
            );

            if (pair is null)
                return BadRequest(new { message = "Código inválido ou expirado." });

            // seta cookie de bypass (trusted device)
            SetBypassCookie(pair.Value.Raw, pair.Value.Exp);

            // dica para o client: agora é só chamar /api/auth/login com as mesmas credenciais
            return NoContent();
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Refresh(CancellationToken ct)
        {
            var raw = Request.Cookies[RefreshCookieName];
            if (string.IsNullOrWhiteSpace(raw)) return Unauthorized();

            var result = await auth.RefreshAsync(
                raw,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                ct
            );

            if (result is null) return Unauthorized();

            SetRefreshCookie(result.RefreshRaw, result.RefreshExpiresUtc);
            return Ok(result.Response);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(CancellationToken ct)
        {
            await auth.LogoutAsync(Request.Cookies[RefreshCookieName], ct);
            Response.Cookies.Delete(RefreshCookieName, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });
            // Se quiser também apagar o bypass deste device:
            // Response.Cookies.Delete(BypassCookieName, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });
            return NoContent();
        }

        private void SetRefreshCookie(string rawToken, DateTime expiresUtc)
        {
            Response.Cookies.Append(RefreshCookieName, rawToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = env.IsDevelopment() ? SameSiteMode.None : SameSiteMode.Strict,
                Expires = expiresUtc,
                Path = "/"
            });
        }

        private void SetBypassCookie(string rawToken, DateTime expiresUtc)
        {
            Response.Cookies.Append(BypassCookieName, rawToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = env.IsDevelopment() ? SameSiteMode.None : SameSiteMode.Strict,
                Expires = expiresUtc,
                Path = "/"
            });
        }
    }
}
