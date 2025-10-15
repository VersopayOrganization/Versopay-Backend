using Microsoft.AspNetCore.Identity;
using VersopayBackend.Auth;
using VersopayBackend.Common;
using VersopayBackend.Dtos;
using VersopayBackend.Repositories;
using VersopayBackend.Repositories.NovaSenha;
using VersopayBackend.Services.Email;
using VersopayBackend.Services.Taxas;
using VersopayBackend.Utils;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;
using static VersopayBackend.Dtos.PasswordResetDtos;

namespace VersopayBackend.Services.Auth
{
    public sealed class AuthService(
        IUsuarioRepository usuarioRepository,
        ITokenService tokens,
        IDeviceTrustChallengeRepository deviceTrustRepo,
        IRefreshTokenService refreshTokenService,
        IPasswordHasher<Usuario> hasher,
        IClock clock,
        ILogger<AuthService> logger,
        INovaSenhaRepository novaSenhaRepository,
        IEmailEnvioService emailEnvio,
        IConfiguration configuration,
        IBypassTokenRepository bypassRepo,
        IUsuarioSenhaHistoricoRepository usuarioSenhaHistoricoRepository,
        IPedidoReadRepository pedidoReadRepo,
        IExtratoRepository extratoRepo,
        ITaxasProvider fees
    ) : IAuthService
    {
        // Mantém o bypass recém-criado para o controller setar o cookie
        private (string Raw, DateTime Exp)? _pendingBypassCookie;

        // ================== LOGIN ==================
        public async Task<LoginOutcomeDto> LoginOrChallengeAsync(
            LoginDto dto, string? ip, string? ua, string? bypassRaw, CancellationToken ct)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var usuario = await usuarioRepository.GetByEmailAsync(email, ct);
            if (usuario is null)
                return new LoginOutcomeDto { ChallengeRequired = false, Auth = null };

            var verify = hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, dto.Senha);
            if (verify == PasswordVerificationResult.Failed)
                return new LoginOutcomeDto { ChallengeRequired = false, Auth = null };

            if (verify == PasswordVerificationResult.SuccessRehashNeeded)
            {
                usuario.SenhaHash = hasher.HashPassword(usuario, dto.Senha);
                await usuarioRepository.SaveChangesAsync(ct);
            }

            // 1) valida cookie bypass (trusted device)
            var hasValidBypass = false;
            if (!string.IsNullOrWhiteSpace(bypassRaw))
            {
                var bpHash = refreshTokenService.Hash(bypassRaw);
                var bp = await bypassRepo.GetByHashWithUserAsync(bpHash, ct);
                if (bp is not null && bp.EstaAtivo && bp.UsuarioId == usuario.Id)
                {
                    hasValidBypass = true;
                    bp.UltimoUsoUtc = clock.UtcNow;
                    await bypassRepo.SaveChangesAsync(ct);
                }
            }

            // 2) se não tem bypass => challenge 2FA por e-mail
            if (!hasValidBypass)
            {
                try
                {
                    var ch = await StartDeviceTrustAsync(usuario.Id, ip, ua, ct);
                    return new LoginOutcomeDto { ChallengeRequired = true, Challenge = ch };
                }
                catch (InvalidOperationException ex)
                {
                    logger.LogWarning(ex, "Falha ao iniciar 2FA para usuário {UserId}", usuario.Id);
                    throw;
                }
            }

            // 3) bypass OK => emite tokens
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

            var usuarioDto = BuildUsuarioResponse(usuario);

            var resp = new AuthResponseDto
            {
                AccessToken = access,
                ExpiresAtUtc = accessExp,
                Usuario = usuarioDto
            };

            return new LoginOutcomeDto
            {
                ChallengeRequired = false,
                Auth = resp,
                RefreshRaw = rawRt,
                RefreshExpiresUtc = expRt
            };
        }

        // ================== REFRESH ==================
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
                Usuario = BuildUsuarioResponse(usuario)
            };

            return new AuthResult(resp, rawNew, expNew);
        }

        // ================== LOGOUT ==================
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

        // ============== RESET DE SENHA (solicitar) ==============
        public async Task ResetSenhaRequestAsync(
            SenhaEsquecidaRequest dto,
            string baseResetUrl,
            string? ip,
            string? ua,
            CancellationToken ct)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var user = await usuarioRepository.GetByEmailAsync(email, ct);
            if (user is null) return;

            await novaSenhaRepository.InvalidateUserTokensAsync(user.Id, ct);

            var horarioAtual = DateTimeBrazil.Now();
            var horarioExpiracao = horarioAtual.AddMinutes(30);

            var (raw, hash, _) = refreshTokenService.Create(TimeSpan.FromMinutes(30));

            await novaSenhaRepository.AddAsync(new NovaSenhaResetToken
            {
                UsuarioId = user.Id,
                TokenHash = hash,
                DataSolicitacao = horarioAtual,
                DataExpiracao = horarioExpiracao,
                Ip = ip,
                UserAgent = ua
            }, ct);

            await novaSenhaRepository.SaveChangesAsync(ct);

            var resetBase = string.IsNullOrWhiteSpace(baseResetUrl)
                ? configuration["Frontend:ResetUrl"] ?? "http://localhost:4200/auth/reset"
                : baseResetUrl;

            var link = $"{resetBase}?token={Uri.EscapeDataString(raw)}";

            try
            {
                using var emailCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await emailEnvio.EnvioResetSenhaAsync(user.Email, user.Nome, link, emailCts.Token);
            }
            catch (OperationCanceledException oce)
            {
                logger.LogWarning(oce, "Timeout ao enviar e-mail de reset de senha para {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao enviar e-mail de reset de senha para {UserId}", user.Id);
            }
        }

        public async Task<bool> ValidarTokenResetSenhaAsync(string rawToken, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(rawToken)) return false;
            var hash = refreshTokenService.Hash(rawToken);
            var hashUsuario = await novaSenhaRepository.GetByHashWithUserAsync(hash, ct);
            return hashUsuario is not null && hashUsuario.EstaAtivo(DateTimeBrazil.Now());
        }

        public async Task<bool> ResetSenhaAsync(RedefinirSenhaRequest dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.Token) ||
                string.IsNullOrWhiteSpace(dto.NovaSenha) ||
                dto.NovaSenha != dto.Confirmacao)
                return false;

            if (!ValidacaoPadraoSenha.IsValido(dto.NovaSenha))
                return false;

            var hash = refreshTokenService.Hash(dto.Token);
            var hashUsuario = await novaSenhaRepository.GetByHashWithUserAsync(hash, ct);
            if (hashUsuario is null || !hashUsuario.EstaAtivo(DateTimeBrazil.Now()))
                return false;

            var user = hashUsuario.Usuario;

            // impede reuso de senhas
            var historicos = await usuarioSenhaHistoricoRepository.GetByUsuarioAsync(user.Id, ct);
            foreach (var hist in historicos)
            {
                var result = hasher.VerifyHashedPassword(user, hist.SenhaHash, dto.NovaSenha);
                if (result == PasswordVerificationResult.Success)
                    return false;
            }

            user.SenhaHash = hasher.HashPassword(user, dto.NovaSenha);
            hashUsuario.DataTokenUsado = DateTimeBrazil.Now();

            await usuarioSenhaHistoricoRepository.AddAsync(new UsuarioSenhaHistorico
            {
                Id = Guid.NewGuid(),
                UsuarioId = user.Id,
                SenhaHash = user.SenhaHash,
                DataCriacao = DateTimeBrazil.Now()
            }, ct);

            await novaSenhaRepository.SaveChangesAsync(ct);
            return true;
        }

        // ============== 2FA / DEVICE TRUST ==============
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

            try
            {
                using var emailCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await emailEnvio.EnvioCodigo2FAAsync(user.Email, user.Nome, code, emailCts.Token);
            }
            catch (OperationCanceledException oce)
            {
                #if DEBUG
                logger.LogWarning(oce, "Timeout ao enviar 2FA; código para {Email}: {Code}", user.Email, code);
                #else
                logger.LogWarning(oce, "Timeout ao enviar código 2FA para {UsuarioId}", usuarioId);
                throw new InvalidOperationException("Falha ao enviar o e-mail de verificação (timeout). Tente novamente.");
                #endif
            }
            catch (Exception ex)
            {
                #if DEBUG
                logger.LogError(ex, "Erro ao enviar 2FA; código para {Email}: {Code}", user.Email, code);
                #else
                logger.LogError(ex, "Erro ao enviar código 2FA para {UsuarioId}", usuarioId);
                throw new InvalidOperationException("Falha ao enviar o e-mail de verificação. Tente novamente.");
                #endif
            }

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

            // cria o bypass token (60 dias)
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

            return (raw, exp);
        }

        public (string Raw, DateTime Exp)? ConsumePendingBypassCookie()
        {
            var v = _pendingBypassCookie;
            _pendingBypassCookie = null;
            return v;
        }

        public async Task<AuthWithPanelsResult?> ConfirmDeviceTrustAndIssueTokensAsync(
            Guid challengeId, string code, string? ip, string? ua, CancellationToken ct)
        {
            // 1) valida challenge + marca como usado
            var ch = await deviceTrustRepo.GetAsync(challengeId, ct);
            if (ch is null || ch.Used || ch.ExpiresAtUtc <= clock.UtcNow) return null;

            var ok = refreshTokenService.Hash(code) == ch.CodeHash;
            if (!ok) return null;

            ch.Used = true;
            await deviceTrustRepo.SaveChangesAsync(ct);

            // 2) cria BypassToken (trusted device) e guarda pra controller setar cookie
            var (rawByp, hashByp, expByp) = refreshTokenService.Create(TimeSpan.FromDays(60));
            await bypassRepo.AddAsync(new BypassToken
            {
                UsuarioId = ch.UsuarioId,
                TokenHash = hashByp,
                ExpiraEmUtc = expByp,
                Ip = ip,
                UserAgent = ua
            }, ct);
            await bypassRepo.SaveChangesAsync(ct);
            _pendingBypassCookie = (rawByp, expByp);

            // 3) carrega usuário
            var usuario = await usuarioRepository.GetByIdAsync(ch.UsuarioId, ct);
            if (usuario is null) return null;

            // 4) emite access + refresh
            var access = tokens.CreateToken(usuario, clock.UtcNow, out var accessExp);
            var (rawRt, hashRt, expRt) = refreshTokenService.Create(TimeSpan.FromDays(7));
            await usuarioRepository.AddRefreshAsync(new RefreshToken
            {
                UsuarioId = usuario.Id,
                TokenHash = hashRt,
                ExpiraEmUtc = expRt,
                Ip = ip,
                UserAgent = ua
            }, ct);
            await usuarioRepository.SaveChangesAsync(ct);

            // 5) Auth
            var authDto = new AuthResponseDto
            {
                AccessToken = access,
                ExpiresAtUtc = accessExp,
                Usuario = BuildUsuarioResponse(usuario)
            };

            // 6) PERFIL (resumo)
            var (qtdVendas, totalVendas) =
                await pedidoReadRepo.GetVendasAprovadasAsync(usuario.Id, null, null, ct);

            var perfil = new PerfilResumoDto
            {
                Nome = usuario.Nome,
                Email = usuario.Email,
                Telefone = usuario.Telefone,
                Instagram = usuario.Instagram,
                Cpf = DocumentoFormatter.Mask(usuario.Cpf),
                Cnpj = DocumentoFormatter.Mask(usuario.Cnpj),
                VendasQtd = qtdVendas,
                VendasTotal = totalVendas
            };

            // 7) DASHBOARD
            var extrato = await extratoRepo.GetByClienteIdAsync(usuario.Id, ct);
            var saldoDisp = extrato?.SaldoDisponivel ?? 0m;
            var saldoPend = extrato?.SaldoPendente ?? 0m;

            static decimal Rate(int aprov, int total) => total > 0 ? Math.Round((decimal)aprov * 100m / total, 2) : 0m;

            var cartao = await pedidoReadRepo.GetStatsPorMetodoAsync(usuario.Id, MetodoPagamento.Cartao, null, null, ct);
            var pix = await pedidoReadRepo.GetStatsPorMetodoAsync(usuario.Id, MetodoPagamento.Pix, null, null, ct);
            var boleto = await pedidoReadRepo.GetStatsPorMetodoAsync(usuario.Id, MetodoPagamento.Boleto, null, null, ct);

            var totalPedidos = cartao.QtdTotal + pix.QtdTotal + boleto.QtdTotal;

            var (qtdCbk, totalCbk) = await pedidoReadRepo.GetChargebackAsync(usuario.Id, null, null, ct);
            var percCbk = totalPedidos > 0 ? Math.Round((decimal)qtdCbk * 100m / totalPedidos, 2) : 0m;

            var dashboard = new DashboardResumoDto
            {
                FaturamentoPeriodo = totalVendas,
                SaldoDisponivel = saldoDisp,
                SaldoPendente = saldoPend,
                Cartao = new MetodoAprovacaoDto
                {
                    PercentAprovacao = Rate(cartao.QtdAprovado, cartao.QtdTotal),
                    QtdAprovado = cartao.QtdAprovado,
                    TotalAprovado = cartao.TotalAprovado,
                    QtdTotal = cartao.QtdTotal,
                    Total = cartao.Total
                },
                Pix = new MetodoAprovacaoDto
                {
                    PercentAprovacao = Rate(pix.QtdAprovado, pix.QtdTotal),
                    QtdAprovado = pix.QtdAprovado,
                    TotalAprovado = pix.TotalAprovado,
                    QtdTotal = pix.QtdTotal,
                    Total = pix.Total
                },
                Boleto = new MetodoAprovacaoDto
                {
                    PercentAprovacao = Rate(boleto.QtdAprovado, boleto.QtdTotal),
                    QtdAprovado = boleto.QtdAprovado,
                    TotalAprovado = boleto.TotalAprovado,
                    QtdTotal = boleto.QtdTotal,
                    Total = boleto.Total
                },
                Chargeback = new ChargebackResumoDto
                {
                    PercentualSobreTotalPedidos = percCbk,
                    Qtd = qtdCbk,
                    Total = totalCbk
                }
            };

            // 8) TAXAS
            var taxas = fees.Get();

            var payload = new AuthWithPanelsDto
            {
                Auth = authDto,
                Perfil = perfil,
                Dashboard = dashboard,
                Taxas = taxas
            };

            return new AuthWithPanelsResult(payload, rawRt, expRt);
        }

        public async Task SendWelcomeEmail(string email, string nome, CancellationToken ct)
        {
            await emailEnvio.EnvioBoasVindasAsync(email, nome, ct);
        }

        // ================== HELPERS ==================
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

        private static UsuarioResponseDto BuildUsuarioResponse(Usuario u)
        {
            return new UsuarioResponseDto
            {
                Id = u.Id,
                Nome = u.Nome,
                Email = u.Email,
                TipoCadastro = u.TipoCadastro,
                Instagram = u.Instagram,
                Telefone = u.Telefone,
                CreatedAt = u.DataCriacao,

                // Doc separados
                Cpf = u.Cpf,
                Cnpj = u.Cnpj,
                CpfFormatado = DocumentoFormatter.Mask(u.Cpf),
                CnpjFormatado = DocumentoFormatter.Mask(u.Cnpj),

                IsAdmin = u.IsAdmin,

                // Perfil
                NomeFantasia = u.NomeFantasia,
                RazaoSocial = u.RazaoSocial,
                Site = u.Site,

                // Endereço
                EnderecoCep = u.EnderecoCep,
                EnderecoLogradouro = u.EnderecoLogradouro,
                EnderecoNumero = u.EnderecoNumero,
                EnderecoComplemento = u.EnderecoComplemento,
                EnderecoBairro = u.EnderecoBairro,
                EnderecoCidade = u.EnderecoCidade,
                EnderecoUF = u.EnderecoUF,

                // Financeiro
                NomeCompletoBanco = u.NomeCompletoBanco,
                CpfCnpjDadosBancarios = u.CpfCnpjDadosBancarios,
                CpfCnpjDadosBancariosFormatado = DocumentoFormatter.Mask(u.CpfCnpjDadosBancarios),
                ChavePix = u.ChavePix,
                ChaveCarteiraCripto = u.ChaveCarteiraCripto,

                CadastroCompleto = u.CadastroCompleto
            };
        }
    }
}
