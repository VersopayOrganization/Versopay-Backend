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

namespace VersopayBackend.Services
{
    public sealed class UsuariosService(
        IUsuarioRepository usuarioRepository,
        IUsuarioSenhaHistoricoRepository usuarioSenhaHistoricoRepository,
        INovaSenhaRepository novaSenhaRepository,
        IRefreshTokenService refreshTokenService,
        IEmailEnvioService emailEnvio,
        IPasswordHasher<Usuario> hasher,
        IConfiguration configuration
        ) : IUsuariosService
    {
        public async Task<UsuarioResponseDto> CadastroInicialAsync(UsuarioCreateDto dto, CancellationToken ct)
        {
            var email = dto.Email.Trim().ToLowerInvariant();

            if (await usuarioRepository.EmailExistsAsync(email, ct))
                throw new InvalidOperationException("Email já cadastrado.");

            var usuario = new Usuario
            {
                Nome = dto.Nome.Trim(),
                Email = email,
                CadastroCompleto = false
            };

            var _hasher = new PasswordHasher<Usuario>();
            usuario.SenhaHash = _hasher.HashPassword(usuario, dto.Senha);

            await usuarioRepository.AddAsync(usuario, ct);
            await usuarioRepository.SaveChangesAsync(ct);

            return usuario.ToResponseDto();
        }

        public async Task<UsuarioResponseDto?> CompletarCadastroAsync(UsuarioCompletarCadastroDto dto, CancellationToken ct)
        {
            var usuario = await usuarioRepository.FindByIdAsync(dto.Id, ct);
            if (usuario is null) return null;

            // Normaliza e valida de acordo com o tipo
            if (dto.TipoCadastro == TipoCadastro.PF)
            {
                var digits = new string((dto.Cpf ?? "").Where(char.IsDigit).ToArray());
                if (digits.Length != 11)
                    throw new ArgumentException("CPF deve ter 11 dígitos.");

                // Unicidade (só se mudou)
                if (!string.Equals(usuario.Cpf, digits, StringComparison.Ordinal))
                {
                    if (await usuarioRepository.CpfExistsAsync(digits, ct))
                        throw new InvalidOperationException("CPF já cadastrado.");
                }

                usuario.Cpf = digits;
                usuario.Cnpj = null;
            }
            else // PJ
            {
                var digits = new string((dto.Cnpj ?? "").Where(char.IsDigit).ToArray());
                if (digits.Length != 14)
                    throw new ArgumentException("CNPJ deve ter 14 dígitos.");

                if (!string.Equals(usuario.Cnpj, digits, StringComparison.Ordinal))
                {
                    if (await usuarioRepository.CnpjExistsAsync(digits, ct))
                        throw new InvalidOperationException("CNPJ já cadastrado.");
                }

                usuario.Cnpj = digits;
                usuario.Cpf = null;
            }

            usuario.TipoCadastro = dto.TipoCadastro;
            usuario.Instagram = string.IsNullOrWhiteSpace(dto.Instagram)
                ? null
                : (dto.Instagram.StartsWith("@") ? dto.Instagram.Trim() : "@" + dto.Instagram.Trim());
            usuario.Telefone = dto.Telefone;
            usuario.DataAtualizacao = DateTime.UtcNow;
            usuario.CadastroCompleto = true;

            await usuarioRepository.SaveChangesAsync(ct);

            // Mapper já deve retornar Cpf/Cnpj separados/mascarados conforme você configurou
            return usuario.ToResponseDto();
        }


        public async Task<IEnumerable<UsuarioResponseDto>> GetAllAsync(CancellationToken ct)
        {
            var usuarios = await usuarioRepository.GetAllNoTrackingAsync(ct);
            // ToResponseDto já aplica máscaras individuais
            return usuarios.Select(u => u.ToResponseDto()).ToList();
        }

        public async Task<UsuarioResponseDto?> GetByIdAsync(int id, CancellationToken ct)
        {
            var usuario = await usuarioRepository.GetByIdNoTrackingAsync(id, ct);
            return usuario?.ToResponseDto();
        }

        public async Task<UsuarioResponseDto?> UpdateAsync(int id, UsuarioUpdateDto dto, CancellationToken ct)
        {
            var usuario = await usuarioRepository.GetByIdAsync(id, ct);
            if (usuario is null) return null;

            // e-mail
            var newEmail = dto.Email.Trim().ToLowerInvariant();
            if (!string.Equals(newEmail, usuario.Email, StringComparison.OrdinalIgnoreCase))
            {
                var exists = await usuarioRepository.GetByEmailAsync(newEmail, ct);
                if (exists is not null && exists.Id != usuario.Id)
                    throw new ArgumentException("Este e-mail já está em uso.");
                usuario.Email = newEmail;
            }

            usuario.Nome = dto.Nome.Trim();
            usuario.TipoCadastro = dto.TipoCadastro;

            // documentos (separados)
            var cpf = new string((dto.Cpf ?? "").Where(char.IsDigit).ToArray());
            var cnpj = new string((dto.Cnpj ?? "").Where(char.IsDigit).ToArray());

            if (dto.TipoCadastro == TipoCadastro.PF)
            {
                if (!string.Equals(usuario.Cpf, cpf, StringComparison.Ordinal) && !string.IsNullOrEmpty(cpf))
                {
                    if (await usuarioRepository.CpfExistsAsync(cpf, ct))
                        throw new ArgumentException("CPF já cadastrado.");
                }
                usuario.Cpf = string.IsNullOrEmpty(cpf) ? null : cpf;
                usuario.Cnpj = null;
            }
            else // PJ
            {
                if (!string.Equals(usuario.Cnpj, cnpj, StringComparison.Ordinal) && !string.IsNullOrEmpty(cnpj))
                {
                    if (await usuarioRepository.CnpjExistsAsync(cnpj, ct))
                        throw new ArgumentException("CNPJ já cadastrado.");
                }
                usuario.Cnpj = string.IsNullOrEmpty(cnpj) ? null : cnpj;
                usuario.Cpf = null;
            }

            // Social / contato
            usuario.Instagram = string.IsNullOrWhiteSpace(dto.Instagram) ? null : dto.Instagram.Trim();
            usuario.Telefone = string.IsNullOrWhiteSpace(dto.Telefone) ? null : dto.Telefone.Trim();

            // Perfil
            usuario.NomeFantasia = string.IsNullOrWhiteSpace(dto.NomeFantasia) ? null : dto.NomeFantasia.Trim();
            usuario.RazaoSocial = string.IsNullOrWhiteSpace(dto.RazaoSocial) ? null : dto.RazaoSocial.Trim();
            usuario.Site = string.IsNullOrWhiteSpace(dto.Site) ? null : dto.Site.Trim();

            // Endereço
            usuario.EnderecoCep = string.IsNullOrWhiteSpace(dto.EnderecoCep) ? null : dto.EnderecoCep.Trim();
            usuario.EnderecoLogradouro = string.IsNullOrWhiteSpace(dto.EnderecoLogradouro) ? null : dto.EnderecoLogradouro.Trim();
            usuario.EnderecoNumero = string.IsNullOrWhiteSpace(dto.EnderecoNumero) ? null : dto.EnderecoNumero.Trim();
            usuario.EnderecoComplemento = string.IsNullOrWhiteSpace(dto.EnderecoComplemento) ? null : dto.EnderecoComplemento.Trim();
            usuario.EnderecoBairro = string.IsNullOrWhiteSpace(dto.EnderecoBairro) ? null : dto.EnderecoBairro.Trim();
            usuario.EnderecoCidade = string.IsNullOrWhiteSpace(dto.EnderecoCidade) ? null : dto.EnderecoCidade.Trim();
            usuario.EnderecoUF = string.IsNullOrWhiteSpace(dto.EnderecoUF) ? null : dto.EnderecoUF.Trim().ToUpperInvariant();

            // Financeiro
            usuario.NomeCompletoBanco = string.IsNullOrWhiteSpace(dto.NomeCompletoBanco) ? null : dto.NomeCompletoBanco.Trim();
            var bankDigits = new string((dto.CpfCnpjDadosBancarios ?? "").Where(char.IsDigit).ToArray());
            usuario.CpfCnpjDadosBancarios = string.IsNullOrWhiteSpace(bankDigits) ? null : bankDigits;
            usuario.ChavePix = string.IsNullOrWhiteSpace(dto.ChavePix) ? null : dto.ChavePix.Trim();
            usuario.ChaveCarteiraCripto = string.IsNullOrWhiteSpace(dto.ChaveCarteiraCripto) ? null : dto.ChaveCarteiraCripto.Trim();

            usuario.DataAtualizacao = DateTime.UtcNow;

            await usuarioRepository.SaveChangesAsync(ct);

            return usuario.ToResponseDto();
        }

        // ======= senha (inalterado) =======
        public async Task<string> ResetSenhaRequestAsync(SenhaEsquecidaRequest req, string baseResetUrl, string? ip, string? userAgent, CancellationToken ct)
        {
            var email = req.Email.Trim().ToLowerInvariant();
            var user = await usuarioRepository.GetByEmailAsync(email, ct);
            if (user is null) return "";

            await novaSenhaRepository.InvalidateUserTokensAsync(user.Id, ct);

            var agora = DateTimeBrazil.Now();
            var expira = agora.AddMinutes(30);

            var (raw, hash, _) = refreshTokenService.Create(TimeSpan.FromMinutes(30));
            await novaSenhaRepository.AddAsync(new NovaSenhaResetToken
            {
                UsuarioId = user.Id,
                TokenHash = hash,
                DataSolicitacao = agora,
                DataExpiracao = expira,
                Ip = ip,
                UserAgent = userAgent
            }, ct);

            await novaSenhaRepository.SaveChangesAsync(ct);

            var link = $"{baseResetUrl}?token={Uri.EscapeDataString(raw)}";
            await emailEnvio.EnvioResetSenhaAsync(user.Email, user.Nome, link, ct);

            return link;
        }

        public async Task<bool> ValidarTokenResetSenhaAsync(string rawToken, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(rawToken)) return false;
            var hash = refreshTokenService.Hash(rawToken);
            var hashUsuario = await novaSenhaRepository.GetByHashWithUserAsync(hash, ct);
            return hashUsuario is not null && hashUsuario.EstaAtivo(DateTimeBrazil.Now());
        }

        public async Task<bool> ResetSenhaAsync(RedefinirSenhaRequest req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.Token) ||
                string.IsNullOrWhiteSpace(req.NovaSenha) ||
                req.NovaSenha != req.Confirmacao)
                return false;

            if (!ValidacaoPadraoSenha.IsValido(req.NovaSenha))
                return false;

            var hash = refreshTokenService.Hash(req.Token);
            var hashUsuario = await novaSenhaRepository.GetByHashWithUserAsync(hash, ct);
            if (hashUsuario is null || !hashUsuario.EstaAtivo(DateTimeBrazil.Now()))
                return false;

            var user = hashUsuario.Usuario;

            var historicos = await usuarioSenhaHistoricoRepository.GetByUsuarioAsync(user.Id, ct);
            foreach (var hist in historicos)
            {
                var result = hasher.VerifyHashedPassword(user, hist.SenhaHash, req.NovaSenha);
                if (result == PasswordVerificationResult.Success)
                    return false;
            }

            user.SenhaHash = hasher.HashPassword(user, req.NovaSenha);
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
    }
}
