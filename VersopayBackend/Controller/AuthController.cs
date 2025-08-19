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
    public class AuthController(AppDbContext db, ITokenService tokens) : ControllerBase
    {
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
            if (u is null)
                return Unauthorized(new { message = "Credenciais inválidas." });

            var hasher = new PasswordHasher<Usuario>();
            var result = hasher.VerifyHashedPassword(u, u.SenhaHash, dto.Senha);
            if (result == PasswordVerificationResult.Failed)
                return Unauthorized(new { message = "Credenciais inválidas." });

            // upgrade hash se necessário
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                u.SenhaHash = hasher.HashPassword(u, dto.Senha);
                await db.SaveChangesAsync();
            }

            var now = DateTime.UtcNow;
            var token = tokens.CreateToken(u, now, out var exp);

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
                AccessToken = token,
                ExpiresAtUtc = exp,
                Usuario = usuarioDto
            });
        }
    }
}
