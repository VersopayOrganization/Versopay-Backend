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
        IDeviceTrustChallengeRepository deviceTrustRepo,
        IRefreshTokenService refreshTokenService,
        IPasswordHasher<Usuario> hasher,
        IClock clock,
<<<<<<< HEAD
        ILogger<AuthService> logger,
        INovaSenhaRepository novaSenhaRepository,
        IEmailEnvioService emailEnvio,
        IConfiguration configuration,
        IBypassTokenRepository bypassRepo,
        IUsuarioSenhaHistoricoRepository usuarioSenhaHistoricoRepository
    ) : IAuthService
=======
        ILogger<AuthService> logger
        ) : IAuthService
>>>>>>> master
    {
        // Mantém seu LoginAsync antigo chamando o novo sem bypassRaw
        public Task<AuthResult?> LoginAsync(LoginDto dto, string? ip, string? ua, CancellationToken ct)
            => LoginAsync(dto, ip, ua, bypassRaw: null, ct);

        public async Task<AuthResult?> LoginAsync(
            LoginDto dto, string? ip, string? ua, string? bypassRaw, CancellationToken ct)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var usuario = await usuarioRepository.GetByEmailAsync(email, ct);
            if (usuario is null) return null;

            var verify = hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, dto.Senha);
            if (verify == PasswordVerificationResult.Failed) return null;
            if (verify == PasswordVerificationResult.SuccessRehashNeeded)
            {
                usuario.SenhaHash = hasher.HashPassword(usuario, dto.Senha);
                await usuarioRepository.SaveChangesAsync(ct);
            }

            // ===== Trusted Device Bypass =====
            var bypassOk = false;
            if (!string.IsNullOrWhiteSpace(bypassRaw))
            {
                var bpHash = refreshTokenService.Hash(bypassRaw);
                var bp = await bypassRepo.GetByHashWithUserAsync(bpHash, ct);
                if (bp is not null && bp.EstaAtivo && bp.UsuarioId == usuario.Id)
                {
                    // opcional: validar IP/UA parecidos
                    bypassOk = true;
                    bp.UltimoUsoUtc = clock.UtcNow;
                    await bypassRepo.SaveChangesAsync(ct);
                }
            }

            // Se não tinha cookie válido, cria um novo (trust this device)
            if (!bypassOk)
            {
                var life = dto.Lembrar7Dias ? TimeSpan.FromDays(90) : TimeSpan.FromDays(30);
                var (raw, hash, exp) = refreshTokenService.Create(life);

                await bypassRepo.AddAsync(new BypassToken
                {
                    UsuarioId = usuario.Id,
                    TokenHash = hash,
                    ExpiraEmUtc = exp,
                    Ip = ip,
                    UserAgent = ua,
                    Dispositivo = null
                }, ct);

                await bypassRepo.SaveChangesAsync(ct);

                // Guardamos o raw para o controller setar no cookie
                _pendingBypassCookie = (raw, exp);
            }

            // Emite Access + Refresh como você já faz
            var access = tokens.CreateToken(usuario, clock.UtcNow, out var accessExp);
            var lifetime = dto.Lembrar7Dias ? TimeSpan.FromDays(7) : TimeSpan.FromDays(1);
            var (rawRt, hashRt, expRt) = refreshTokenService.Create(lifetime);

            await usuarioRepository.AddRefreshAsync(new RefreshToken
            {
                UsuarioId = usuario.Id,
                TokenHash = hashRt,
                ExpiraEmUtc = expRt,
                Ip = ip,
                UserAgent = ua
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

            var resp = new AuthResponseDto
            {
                AccessToken = access,
                ExpiresAtUtc = accessExp,
                Usuario = usuarioDto
            };

            return new AuthResult(resp, rawRt, expRt);
        }

        // ===== pequeno truque para passar o novo cookie p/ o controller =====
        private (string Raw, DateTime Exp)? _pendingBypassCookie;

        public (string Raw, DateTime Exp)? ConsumePendingBypassCookie()
        {
            var v = _pendingBypassCookie;
            _pendingBypassCookie = null;
            return v;
        }

        public async Task<AuthResult?> RefreshAsync(
            string rawRefresh, string? ip, string? userAgent, CancellationToken ct)
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
<<<<<<< HEAD

        public async Task ResetSenhaRequestAsync(
            SenhaEsquecidaRequest senhaEsquecidaRequest,
            string baseResetUrl,
            string? ip,
            string? userAgent,
            CancellationToken cancellationToken)
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

        public async Task<bool> ResetSenhaAsync(
            RedefinirSenhaRequest redefinirSenhaRequest, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(redefinirSenhaRequest.Token) ||
                string.IsNullOrWhiteSpace(redefinirSenhaRequest.NovaSenha) ||
                redefinirSenhaRequest.NovaSenha != redefinirSenhaRequest.Confirmacao)
                return false;

            if (!ValidacaoPadraoSenha.IsValido(redefinirSenhaRequest.NovaSenha))
                return false;

            var hash = refreshTokenService.Hash(redefinirSenhaRequest.Token);
            var hashUsuario = await novaSenhaRepository.GetByHashWithUserAsync(hash, cancellationToken);
            if (hashUsuario is null || !hashUsuario.EstaAtivo(DateTimeBrazil.Now()))
                return false;

            var user = hashUsuario.Usuario;

            var historicos = await usuarioSenhaHistoricoRepository.GetByUsuarioAsync(user.Id, cancellationToken);
            foreach (var hist in historicos)
            {
                var result = hasher.VerifyHashedPassword(user, hist.SenhaHash, redefinirSenhaRequest.NovaSenha);
                if (result == PasswordVerificationResult.Success)
                    return false;
            }

            user.SenhaHash = hasher.HashPassword(user, redefinirSenhaRequest.NovaSenha);
            hashUsuario.DataTokenUsado = DateTimeBrazil.Now();

            await usuarioSenhaHistoricoRepository.AddAsync(new UsuarioSenhaHistorico
            {
                Id = Guid.NewGuid(),
                UsuarioId = user.Id,
                SenhaHash = user.SenhaHash,
                DataCriacao = DateTimeBrazil.Now()
            }, cancellationToken);

            await novaSenhaRepository.SaveChangesAsync(cancellationToken);
            return true;
        }

        // ===== Ativação de device (e-mail ou outro dispositivo) =====
        public async Task<DeviceTrustChallengeDto> StartDeviceTrustAsync(
            int usuarioId, string? ip, string? ua, CancellationToken ct)
        {
            await deviceTrustRepo.InvalidateUserOpenAsync(usuarioId, ct);

            var code = GenerateSixDigitCode();
            var codeHash = refreshTokenService.Hash(code);
            var expires = clock.UtcNow.AddMinutes(10);

            var ch = new DeviceTrustChallenge
            {
                UsuarioId = usuarioId,
                CodeHash = codeHash,
                ExpiresAtUtc = expires,
                Ip = ip,
                UserAgent = ua
            };

            await deviceTrustRepo.AddAsync(ch, ct);
            await deviceTrustRepo.SaveChangesAsync(ct);

            var user = await usuarioRepository.GetByIdAsync(usuarioId, ct)
                       ?? throw new InvalidOperationException("Usuário não encontrado.");

            await emailEnvio.EnvioCodigo2FAAsync(user.Email, user.Nome, code, ct);

            return new DeviceTrustChallengeDto(ch.Id, ch.ExpiresAtUtc, MaskEmail(user.Email));
        }

        public async Task<(string Raw, DateTime Exp)?> ConfirmDeviceTrustAsync(
            Guid challengeId, string code, string? ip, string? ua, CancellationToken ct)
        {
            var ch = await deviceTrustRepo.GetAsync(challengeId, ct);
            if (ch is null || ch.Used || ch.ExpiresAtUtc <= clock.UtcNow) return null;

            var ok = refreshTokenService.Hash(code) == ch.CodeHash;
            if (!ok) return null;

            ch.Used = true;
            await deviceTrustRepo.SaveChangesAsync(ct);

            // Cria agora o BypassToken
            var (raw, hash, exp) = refreshTokenService.Create(TimeSpan.FromDays(60));
            await bypassRepo.AddAsync(new BypassToken
            {
                UsuarioId = ch.UsuarioId,
                TokenHash = hash,
                ExpiraEmUtc = exp,
                Ip = ip,
                UserAgent = ua
            }, ct);

            await bypassRepo.SaveChangesAsync(ct);

            return (raw, exp); // o controller vai setar o cookie bptkn
        }

        private static string GenerateSixDigitCode()
        {
            Span<byte> b = stackalloc byte[4];
            System.Security.Cryptography.RandomNumberGenerator.Fill(b);
            return (BitConverter.ToUInt32(b) % 1_000_000u).ToString("D6");
        }

        private static string MaskEmail(string email)
        {
            var at = email.IndexOf('@');
            if (at <= 1) return "***";

            var name = email[..at];
            var domain = email[(at + 1)..];
            var first = name[0];
            var last = name[^1];

            return $"{first}{new string('*', Math.Max(1, name.Length - 2))}{last}@{domain}";
        }
=======
>>>>>>> master
    }
}
