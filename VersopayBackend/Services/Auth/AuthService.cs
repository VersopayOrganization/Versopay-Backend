using Microsoft.AspNetCore.Identity;
using VersopayBackend.Auth;
using VersopayBackend.Common;
using VersopayBackend.Dtos;
using VersopayBackend.Repositories;
using VersopayBackend.Repositories.NovaSenha;
using VersopayBackend.Services.Email;
using VersopayBackend.Utils;
using VersopayLibrary.Models;
using static VersopayBackend.Dtos.PasswordResetDtos;

namespace VersopayBackend.Services.Auth
{
    public sealed class AuthService(
        IUsuarioRepository usuarioRepository,
        ITokenService tokens,
        IRefreshTokenService refreshTokenService,
        IPasswordHasher<Usuario> hasher,
        IClock clock,
        ILogger<AuthService> logger,
        INovaSenhaRepository novaSenhaRepository,
        IEmailEnvioService emailEnvio,
        IConfiguration configuration) : IAuthService
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

        public async Task ResetSenhaRequestAsync(SenhaEsquecidaRequest senhaEsquecidaRequest, string baseResetUrl, string? ip, string? userAgent, CancellationToken cancellationToken)
        {
            var email = senhaEsquecidaRequest.Email.Trim().ToLowerInvariant();
            var user = await usuarioRepository.GetByEmailAsync(email, cancellationToken);

            if (user is null) return;

            await novaSenhaRepository.InvalidateUserTokensAsync(user.Id, cancellationToken);

            var horarioAtual = DateTimeBrazil.Now();
            var horarioExpiracao = horarioAtual.AddMinutes(30);

            var (raw, hash, expiracao) = refreshTokenService.Create(TimeSpan.FromMinutes(30));
            await novaSenhaRepository.AddAsync(new NovaSenhaResetToken
            {
                UsuarioId = user.Id,
                TokenHash = hash,
                DataSolicitacao = horarioAtual,
                DataExpiracao = horarioExpiracao,
                Ip = ip,
                UserAgent = userAgent
            }, cancellationToken);

            await novaSenhaRepository.SaveChangesAsync(cancellationToken);

            var resetBase = string.IsNullOrWhiteSpace(baseResetUrl)
                ? configuration["Frontend:ResetUrl"] ?? "http://localhost:4200/auth/reset"
                : baseResetUrl;
            var link = $"{resetBase}?token={Uri.EscapeDataString(raw)}";

            await emailEnvio.EnvioResetSenhaAsync(user.Email, user.Nome, link, cancellationToken);
        }

        public async Task<bool> ValidarTokenResetSenhaAsync(string rawToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(rawToken)) return false;
            var hash = refreshTokenService.Hash(rawToken);
            var hashUsuario = await novaSenhaRepository.GetByHashWithUserAsync(hash, cancellationToken);
            return hashUsuario is not null && hashUsuario.EstaAtivo(DateTimeBrazil.Now());
        }

        public async Task<bool> ResetSenhaAsync(RedefinirSenhaRequest redefinirSenhaRequest, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(redefinirSenhaRequest.Token) ||
                string.IsNullOrWhiteSpace(redefinirSenhaRequest.NovaSenha) ||
                redefinirSenhaRequest.NovaSenha != redefinirSenhaRequest.Confirmacao)
                return false;

            if (!ValidacaoPadraoSenha.IsValid(redefinirSenhaRequest.NovaSenha))
                return false;

            var hash = refreshTokenService.Hash(redefinirSenhaRequest.Token);
            var hashUsuario = await novaSenhaRepository.GetByHashWithUserAsync(hash, cancellationToken);
            if (hashUsuario is null || !hashUsuario.EstaAtivo(DateTimeBrazil.Now()))
                return false;

            var user = hashUsuario.Usuario;

            user.SenhaHash = hasher.HashPassword(user, redefinirSenhaRequest.NovaSenha);
            hashUsuario.DataTokenUsado = DateTimeBrazil.Now();

            await novaSenhaRepository.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}