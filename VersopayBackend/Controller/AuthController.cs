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

                // Credenciais ruins
                if (outcome.Auth is null && !outcome.ChallengeRequired)
                    return Unauthorized(new { message = "Credenciais inválidas." });

                // Precisa de 2FA → 202
                if (outcome.ChallengeRequired && outcome.Challenge is not null)
                {
                    return Accepted(new
                    {
                        requires2fa = true,
                        challenge = outcome.Challenge,
                        challengeId = outcome.Challenge.ChallengeId,
                        maskedEmail = outcome.Challenge.MaskedEmail
                    });
                }

                // Tokens OK → 200
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
                // erro controlado no envio do e-mail do 2FA
                return Problem(ex.Message, statusCode: 500);
            }
        }

        /// <summary>
        /// Força o fluxo 2FA: se NÃO houver device confiável, cria challenge e envia código.
        /// Se houver device confiável, retorna tokens (mesma resposta do /login).
        /// </summary>
        [HttpPost("login/2fa/start")]
        [AllowAnonymous]
        public async Task<IActionResult> Login2FAStart([FromBody] LoginDto loginDto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var outcome = await auth.LoginOrChallengeAsync(
                loginDto,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                bypassRaw: null,
                ct
            );

            if (outcome.Auth is null && !outcome.ChallengeRequired)
                return Unauthorized(new { message = "Credenciais inválidas." });

            if (outcome.ChallengeRequired && outcome.Challenge is not null)
            {
                return Accepted(new
                {
                    requires2fa = true,
                    challenge = outcome.Challenge,
                    challengeId = outcome.Challenge.ChallengeId,
                    maskedEmail = outcome.Challenge.MaskedEmail
                });
            }

            // Tokens OK
            if (string.IsNullOrWhiteSpace(outcome.RefreshRaw) || outcome.RefreshExpiresUtc is null)
                return Problem("Falha ao emitir refresh token.", statusCode: 500);

            SetRefreshCookie(outcome.RefreshRaw, outcome.RefreshExpiresUtc.Value);

            var pending = auth.ConsumePendingBypassCookie();
            if (pending is not null)
                SetBypassCookie(pending.Value.Raw, pending.Value.Exp);

            return Ok(outcome.Auth);
        }

        /// <summary>
        /// Confirma o código 2FA. Emite tokens, grava cookies e retorna
        /// Auth + Perfil + Dashboard + Taxas em um único payload.
        /// </summary>
        [HttpPost("login/2fa/confirm")]
        [AllowAnonymous]
        public async Task<IActionResult> Login2FAConfirm([FromBody] DeviceTrustConfirmRequest body, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var result = await auth.ConfirmDeviceTrustAndIssueTokensAsync(
                body.ChallengeId,
                body.Code,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                ct
            );

            if (result is null)
                return BadRequest(new { message = "Código inválido ou expirado." });

            // cookie refresh
            SetRefreshCookie(result.RefreshRaw, result.RefreshExpiresUtc);

            // cookie bypass
            var pending = auth.ConsumePendingBypassCookie();
            if (pending is not null)
                SetBypassCookie(pending.Value.Raw, pending.Value.Exp);

            // payload completo: auth + perfil + dashboard + taxas
            return Ok(result.Payload);
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
            Response.Cookies.Delete(RefreshCookieName, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
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
