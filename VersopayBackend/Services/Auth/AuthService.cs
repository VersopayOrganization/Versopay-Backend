using Microsoft.AspNetCore.Identity;
using VersopayBackend.Auth;
using VersopayBackend.Common;
using VersopayBackend.Dtos;
using VersopayBackend.Repositories;
using VersopayLibrary.Models;

namespace VersopayBackend.Services.Auth
{
    public sealed class AuthService(
        IUsuarioRepository repo,
        ITokenService tokens,
        IRefreshTokenService rts,
        IPasswordHasher<Usuario> hasher,
        IClock clock,
        ILogger<AuthService> logger) : IAuthService
    {
        public async Task<AuthResult?> LoginAsync(LoginDto dto, string? ip, string? userAgent, CancellationToken ct)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var u = await repo.GetByEmailAsync(email, ct);
            if (u is null) return null;

            var vr = hasher.VerifyHashedPassword(u, u.SenhaHash, dto.Senha);
            if (vr == PasswordVerificationResult.Failed) return null;
            if (vr == PasswordVerificationResult.SuccessRehashNeeded)
            {
                u.SenhaHash = hasher.HashPassword(u, dto.Senha);
                await repo.SaveChangesAsync(ct);
            }

            var access = tokens.CreateToken(u, clock.UtcNow, out var accessExp);

            var lifetime = dto.Lembrar7Dias ? TimeSpan.FromDays(7) : TimeSpan.FromDays(1);
            var (raw, hash, exp) = rts.Create(lifetime);

            await repo.AddRefreshAsync(new RefreshToken
            {
                UsuarioId = u.Id,
                TokenHash = hash,
                ExpiraEmUtc = exp,
                Ip = ip,
                UserAgent = userAgent
            }, ct);

            await repo.SaveChangesAsync(ct);

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
                CpfCnpjFormatado = DocumentoFormatter.Mask(u.CpfCnpj),
                IsAdmin = u.IsAdmin
            };

            var resp = new AuthResponseDto { AccessToken = access, ExpiresAtUtc = accessExp, Usuario = usuarioDto };
            return new AuthResult(resp, raw, exp);
        }

        public async Task<AuthResult?> RefreshAsync(string rawRefresh, string? ip, string? userAgent, CancellationToken ct)
        {
            var hash = rts.Hash(rawRefresh);
            var rt = await repo.GetRefreshWithUserByHashAsync(hash, ct);
            if (rt is null || !rt.EstaAtivo) return null;

            rt.RevogadoEmUtc = clock.UtcNow;
            var lifetime = rt.ExpiraEmUtc - rt.CriadoEmUtc;
            var (rawNew, hashNew, expNew) = rts.Create(lifetime);
            rt.SubstituidoPorHash = hashNew;

            await repo.AddRefreshAsync(new RefreshToken
            {
                UsuarioId = rt.UsuarioId,
                TokenHash = hashNew,
                ExpiraEmUtc = expNew,
                Ip = ip,
                UserAgent = userAgent
            }, ct);

            var u = rt.Usuario!;
            var access = tokens.CreateToken(u, clock.UtcNow, out var accessExp);
            await repo.SaveChangesAsync(ct);

            var resp = new AuthResponseDto
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
                    CpfCnpjFormatado = DocumentoFormatter.Mask(u.CpfCnpj),
                    IsAdmin = u.IsAdmin
                }
            };

            return new AuthResult(resp, rawNew, expNew);
        }

        public async Task LogoutAsync(string? rawRefresh, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(rawRefresh)) return;
            var hash = rts.Hash(rawRefresh);
            var rt = await repo.GetRefreshWithUserByHashAsync(hash, ct);
            if (rt is not null && rt.RevogadoEmUtc is null)
            {
                rt.RevogadoEmUtc = clock.UtcNow;
                await repo.SaveChangesAsync(ct);
            }
        }
    }
}