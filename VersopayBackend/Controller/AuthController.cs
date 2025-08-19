using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VersopayBackend.Auth;
using VersopayBackend.Dtos;
using VersopayDatabase.Data;
using VersopayLibrary.Models;

namespace VersopayBackend.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(AppDbContext db, ITokenService tokens, IRefreshTokenService rts) : ControllerBase
    {
        const string RefreshCookieName = "rtkn";

        static string? MaskDocumento(string? d)
        {
            if (string.IsNullOrWhiteSpace(d)) return null;
            var x = new string(d.Where(char.IsDigit).ToArray());
            if (x.Length == 11) return Convert.ToUInt64(x).ToString(@"000\.000\.000\-00");
            if (x.Length == 14) return Convert.ToUInt64(x).ToString(@"00\.000\.000\/0000\-00");
            return x;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var email = dto.Email.Trim().ToLowerInvariant();
            var u = await db.Usuarios.FirstOrDefaultAsync(x => x.Email == email);
            if (u is null) return Unauthorized(new { message = "Credenciais inválidas." });

            var hasher = new PasswordHasher<Usuario>();
            var vr = hasher.VerifyHashedPassword(u, u.SenhaHash, dto.Senha);
            if (vr == PasswordVerificationResult.Failed) return Unauthorized(new { message = "Credenciais inválidas." });
            if (vr == PasswordVerificationResult.SuccessRehashNeeded)
            {
                u.SenhaHash = hasher.HashPassword(u, dto.Senha);
                await db.SaveChangesAsync();
            }

            // Access token (curto)
            var now = DateTime.UtcNow;
            var access = tokens.CreateToken(u, now, out var accessExp);

            // Refresh token (7d se lembrar, senão 1d)
            var lifetime = dto.Lembrar7Dias ? TimeSpan.FromDays(7) : TimeSpan.FromDays(1);
            var (raw, hash, exp) = rts.Create(lifetime);

            db.RefreshTokens.Add(new RefreshToken
            {
                UsuarioId = u.Id,
                TokenHash = hash,
                ExpiraEmUtc = exp,
                Ip = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString()
            });
            await db.SaveChangesAsync();

            SetRefreshCookie(raw, exp);

            var usuarioDto = new UsuarioResponseDto
            {
                Id = u.Id,
                Nome = u.Nome,
                Email = u.Email,
                TipoCadastro = u.TipoCadastro,
                Instagram = u.Instagram,
                Telefone = u.Telefone,
                CreatedAt = u.DataCriacao,
                CpfCnpj = u.CpfCnpj,
                CpfCnpjFormatado = MaskDocumento(u.CpfCnpj)
            };

            return Ok(new AuthResponseDto
            {
                AccessToken = access,
                ExpiresAtUtc = accessExp,
                Usuario = usuarioDto
            });
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Refresh()
        {
            var raw = Request.Cookies[RefreshCookieName];
            if (string.IsNullOrWhiteSpace(raw)) return Unauthorized();

            var hash = rts.Hash(raw);
            var rt = await db.RefreshTokens.Include(x => x.Usuario)
                                           .FirstOrDefaultAsync(x => x.TokenHash == hash);
            if (rt is null || !rt.EstaAtivo) return Unauthorized();

            // Rotação: revogar atual e emitir novo
            rt.RevogadoEmUtc = DateTime.UtcNow;
            var lifetime = (rt.ExpiraEmUtc - rt.CriadoEmUtc); // mantém janela original
            var (rawNew, hashNew, expNew) = rts.Create(lifetime);
            rt.SubstituidoPorHash = hashNew;

            db.RefreshTokens.Add(new RefreshToken
            {
                UsuarioId = rt.UsuarioId,
                TokenHash = hashNew,
                ExpiraEmUtc = expNew,
                Ip = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString()
            });

            // Novo access token
            var now = DateTime.UtcNow;
            var access = tokens.CreateToken(rt.Usuario, now, out var accessExp);

            await db.SaveChangesAsync();
            SetRefreshCookie(rawNew, expNew);

            var u = rt.Usuario;
            return Ok(new AuthResponseDto
            {
                AccessToken = access,
                ExpiresAtUtc = accessExp,
                Usuario = new UsuarioResponseDto
                {
                    Id = u.Id,
                    Nome = u.Nome,
                    Email = u.Email,
                    TipoCadastro = u.TipoCadastro,
                    Instagram = u.Instagram,
                    Telefone = u.Telefone,
                    CreatedAt = u.DataCriacao,
                    CpfCnpj = u.CpfCnpj,
                    CpfCnpjFormatado = MaskDocumento(u.CpfCnpj)
                }
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var raw = Request.Cookies[RefreshCookieName];
            if (!string.IsNullOrWhiteSpace(raw))
            {
                var hash = rts.Hash(raw);
                var rt = await db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash);
                if (rt is not null && rt.RevogadoEmUtc is null)
                {
                    rt.RevogadoEmUtc = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }
            }

            Response.Cookies.Delete(RefreshCookieName, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
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
    }
}
