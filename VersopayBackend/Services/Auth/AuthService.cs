using Microsoft.AspNetCore.Identity;
using VersopayBackend.Auth;
using VersopayBackend.Common;
using VersopayBackend.Dtos;
using VersopayBackend.Repositories;
using VersopayLibrary.Models;

namespace VersopayBackend.Services.Auth
{
    public sealed class AuthService(
        IUsuarioRepository usuarioRepository,
        ITokenService tokens,
        IRefreshTokenService refreshTokenService,
        IPasswordHasher<Usuario> hasher,
        IClock clock,
        ILogger<AuthService> logger
        ) : IAuthService
    {
        public async Task<AuthResult?> LoginAsync(LoginDto dto, string? ip, string? userAgent, CancellationToken ct)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var usuario = await usuarioRepository.GetByEmailAsync(email, ct);
            if (usuario is null) return null;

            var verifyHashPassword = hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, dto.Senha);
            if (verifyHashPassword == PasswordVerificationResult.Failed) return null;
            if (verifyHashPassword == PasswordVerificationResult.SuccessRehashNeeded)
            {
                usuario.SenhaHash = hasher.HashPassword(usuario, dto.Senha);
                await usuarioRepository.SaveChangesAsync(ct);
            }

            var access = tokens.CreateToken(usuario, clock.UtcNow, out var accessExp);

            var lifetime = dto.Lembrar7Dias ? TimeSpan.FromDays(7) : TimeSpan.FromDays(1);
            var (raw, hash, exp) = refreshTokenService.Create(lifetime);

            await usuarioRepository.AddRefreshAsync(new RefreshToken
            {
                UsuarioId = usuario.Id,
                TokenHash = hash,
                ExpiraEmUtc = exp,
                Ip = ip,
                UserAgent = userAgent
            }, ct);

            await usuarioRepository.SaveChangesAsync(ct);

            var usuarioDto = new UsuarioResponseDto
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                TipoCadastro = usuario.TipoCadastro,
                Instagram = usuario.Instagram,
                Telefone = usuario.Telefone,
                CreatedAt = usuario.DataCriacao,
                CpfCnpj = usuario.CpfCnpj,
                CpfCnpjFormatado = DocumentoFormatter.Mask(usuario.CpfCnpj),
                IsAdmin = usuario.IsAdmin
            };

            var resp = new AuthResponseDto { AccessToken = access, ExpiresAtUtc = accessExp, Usuario = usuarioDto };
            return new AuthResult(resp, raw, exp);
        }

        public async Task<AuthResult?> RefreshAsync(string rawRefresh, string? ip, string? userAgent, CancellationToken ct)
        {
            var hash = refreshTokenService.Hash(rawRefresh);
            var refreshUserHash = await usuarioRepository.GetRefreshWithUserByHashAsync(hash, ct);
            if (refreshUserHash is null || !refreshUserHash.EstaAtivo) return null;

            refreshUserHash.RevogadoEmUtc = clock.UtcNow;
            var lifetime = refreshUserHash.ExpiraEmUtc - refreshUserHash.CriadoEmUtc;
            var (rawNew, hashNew, expNew) = refreshTokenService.Create(lifetime);
            refreshUserHash.SubstituidoPorHash = hashNew;

            await usuarioRepository.AddRefreshAsync(new RefreshToken
            {
                UsuarioId = refreshUserHash.UsuarioId,
                TokenHash = hashNew,
                ExpiraEmUtc = expNew,
                Ip = ip,
                UserAgent = userAgent
            }, ct);

            var usuario = refreshUserHash.Usuario!;
            var access = tokens.CreateToken(usuario, clock.UtcNow, out var accessExp);
            await usuarioRepository.SaveChangesAsync(ct);

            var resp = new AuthResponseDto
            {
                AccessToken = access,
                ExpiresAtUtc = accessExp,
                Usuario = new UsuarioResponseDto
                {
                    Id = usuario.Id,
                    Nome = usuario.Nome,
                    Email = usuario.Email,
                    TipoCadastro = usuario.TipoCadastro,
                    Instagram = usuario.Instagram,
                    Telefone = usuario.Telefone,
                    CreatedAt = usuario.DataCriacao,
                    CpfCnpj = usuario.CpfCnpj,
                    CpfCnpjFormatado = DocumentoFormatter.Mask(usuario.CpfCnpj),
                    IsAdmin = usuario.IsAdmin
                }
            };

            return new AuthResult(resp, rawNew, expNew);
        }

        public async Task LogoutAsync(string? rawRefresh, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(rawRefresh)) return;
            var hash = refreshTokenService.Hash(rawRefresh);
            var refreshUserHash = await usuarioRepository.GetRefreshWithUserByHashAsync(hash, ct);
            if (refreshUserHash is not null && refreshUserHash.RevogadoEmUtc is null)
            {
                refreshUserHash.RevogadoEmUtc = clock.UtcNow;
                await usuarioRepository.SaveChangesAsync(ct);
            }
        }
    }
}