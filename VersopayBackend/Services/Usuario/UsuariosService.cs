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
        public async Task<UsuarioResponseDto> CadastroInicialAsync(UsuarioCreateDto usuarioCreateDto, CancellationToken cancellationToken)
        {
            var email = usuarioCreateDto.Email.Trim().ToLowerInvariant();

            if (await usuarioRepository.EmailExistsAsync(email, cancellationToken))
                throw new InvalidOperationException("Email já cadastrado.");

            var usuario = new Usuario
            {
                Nome = usuarioCreateDto.Nome.Trim(),
                Email = email,
                CadastroCompleto = false
            };

            var hasher = new PasswordHasher<Usuario>();
            usuario.SenhaHash = hasher.HashPassword(usuario, usuarioCreateDto.Senha);

            await usuarioRepository.AddAsync(usuario, cancellationToken);
            await usuarioRepository.SaveChangesAsync(cancellationToken);

            return usuario.ToResponseDto();
        }

        public async Task<UsuarioResponseDto?> CompletarCadastroAsync(UsuarioCompletarCadastroDto usuarioCompletarCadastroDto, CancellationToken cancellationToken)
        {
            var usuario = await usuarioRepository.FindByIdAsync(usuarioCompletarCadastroDto.Id, cancellationToken);
            if (usuario is null) return null;

            var digits = CpfCnpjUtils.ValidarQtdDigitos(usuarioCompletarCadastroDto.CpfCnpj!);
            if (!CpfCnpjUtils.IsValidoTipo(digits, usuarioCompletarCadastroDto.TipoCadastro))
                throw new ArgumentException("CpfCnpj não condiz com o TipoCadastro.");

            if (await usuarioRepository.CpfCnpjExistsAsync(digits, cancellationToken) &&
                !string.Equals(usuario.CpfCnpj, digits, StringComparison.Ordinal))
                throw new InvalidOperationException("CPF/CNPJ já cadastrado.");

            usuario.TipoCadastro = usuarioCompletarCadastroDto.TipoCadastro;
            usuario.CpfCnpj = digits;
            usuario.Instagram = string.IsNullOrWhiteSpace(usuarioCompletarCadastroDto.Instagram) ? null :
                (usuarioCompletarCadastroDto.Instagram.StartsWith("@") ? usuarioCompletarCadastroDto.Instagram.Trim() : "@" + usuarioCompletarCadastroDto.Instagram.Trim());
            usuario.Telefone = usuarioCompletarCadastroDto.Telefone;
            usuario.DataAtualizacao = DateTime.UtcNow;
            usuario.CadastroCompleto = true;

            await usuarioRepository.SaveChangesAsync(cancellationToken);

            var response = usuario.ToResponseDto();
            response.CpfCnpjFormatado = CpfCnpjUtils.Mascara(response.CpfCnpj);
            return response;
        }

        public async Task<IEnumerable<UsuarioResponseDto>> GetAllAsync(CancellationToken cancellationToken)
        {
            var usuarios = await usuarioRepository.GetAllNoTrackingAsync(cancellationToken);
            var usuariosLista = usuarios.Select(usuario => usuario.ToResponseDto()).ToList();
            foreach (var usuario in usuariosLista) usuario.CpfCnpjFormatado = CpfCnpjUtils.Mascara(usuario.CpfCnpj);
            return usuariosLista;
        }

        public async Task<UsuarioResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            var usuario = await usuarioRepository.GetByIdNoTrackingAsync(id, cancellationToken);
            if (usuario is null) return null;

            var usuarioResposedto = usuario.ToResponseDto();
            usuarioResposedto.CpfCnpjFormatado = CpfCnpjUtils.Mascara(usuarioResposedto.CpfCnpj);
            return usuarioResposedto;
        }

        public async Task<UsuarioResponseDto?> UpdateAsync(int id, UsuarioUpdateDto usuarioUpdateDto, CancellationToken cancellationToken)
        {
            var usuario = await usuarioRepository.GetByIdAsync(id, cancellationToken);
            if (usuario is null) return null;

            // e-mail: normaliza + verifica conflito
            var newEmail = usuarioUpdateDto.Email.Trim().ToLowerInvariant();
            if (!string.Equals(newEmail, usuario.Email, StringComparison.OrdinalIgnoreCase))
            {
                var exists = await usuarioRepository.GetByEmailAsync(newEmail, cancellationToken);
                if (exists is not null && exists.Id != usuario.Id)
                    throw new ArgumentException("Este e-mail já está em uso.");
                usuario.Email = newEmail;
            }

            usuario.Nome = usuarioUpdateDto.Nome.Trim();
            usuario.TipoCadastro = usuarioUpdateDto.TipoCadastro;

            // sempre salvar somente dígitos no CpfCnpj
            usuario.CpfCnpj = new string((usuarioUpdateDto.CpfCnpj ?? "").Where(char.IsDigit).ToArray());

            usuario.Instagram = string.IsNullOrWhiteSpace(usuarioUpdateDto.Instagram) ? null : usuarioUpdateDto.Instagram.Trim();
            usuario.Telefone = string.IsNullOrWhiteSpace(usuarioUpdateDto.Telefone) ? null : usuarioUpdateDto.Telefone.Trim();

            // Perfil
            usuario.NomeFantasia = string.IsNullOrWhiteSpace(usuarioUpdateDto.NomeFantasia) ? null : usuarioUpdateDto.NomeFantasia.Trim();
            usuario.RazaoSocial = string.IsNullOrWhiteSpace(usuarioUpdateDto.RazaoSocial) ? null : usuarioUpdateDto.RazaoSocial.Trim();
            usuario.Site = string.IsNullOrWhiteSpace(usuarioUpdateDto.Site) ? null : usuarioUpdateDto.Site.Trim();

            // Endereço (CEP só dígitos, mas mantenho até 9 para aceitar “12345-678”)
            usuario.EnderecoCep = string.IsNullOrWhiteSpace(usuarioUpdateDto.EnderecoCep) ? null : usuarioUpdateDto.EnderecoCep.Trim();
            usuario.EnderecoLogradouro = string.IsNullOrWhiteSpace(usuarioUpdateDto.EnderecoLogradouro) ? null : usuarioUpdateDto.EnderecoLogradouro.Trim();
            usuario.EnderecoNumero = string.IsNullOrWhiteSpace(usuarioUpdateDto.EnderecoNumero) ? null : usuarioUpdateDto.EnderecoNumero.Trim();
            usuario.EnderecoComplemento = string.IsNullOrWhiteSpace(usuarioUpdateDto.EnderecoComplemento) ? null : usuarioUpdateDto.EnderecoComplemento.Trim();
            usuario.EnderecoBairro = string.IsNullOrWhiteSpace(usuarioUpdateDto.EnderecoBairro) ? null : usuarioUpdateDto.EnderecoBairro.Trim();
            usuario.EnderecoCidade = string.IsNullOrWhiteSpace(usuarioUpdateDto.EnderecoCidade) ? null : usuarioUpdateDto.EnderecoCidade.Trim();
            usuario.EnderecoUF = string.IsNullOrWhiteSpace(usuarioUpdateDto.EnderecoUF) ? null : usuarioUpdateDto.EnderecoUF.Trim().ToUpperInvariant();

            // Financeiro
            usuario.NomeCompletoBanco = string.IsNullOrWhiteSpace(usuarioUpdateDto.NomeCompletoBanco) ? null : usuarioUpdateDto.NomeCompletoBanco.Trim();
            var bankDigits = new string((usuarioUpdateDto.CpfCnpjDadosBancarios ?? "").Where(char.IsDigit).ToArray());
            usuario.CpfCnpjDadosBancarios = string.IsNullOrWhiteSpace(bankDigits) ? null : bankDigits;
            usuario.ChavePix = string.IsNullOrWhiteSpace(usuarioUpdateDto.ChavePix) ? null : usuarioUpdateDto.ChavePix.Trim();
            usuario.ChaveCarteiraCripto = string.IsNullOrWhiteSpace(usuarioUpdateDto.ChaveCarteiraCripto) ? null : usuarioUpdateDto.ChaveCarteiraCripto.Trim();

            usuario.DataAtualizacao = DateTime.UtcNow;

            await usuarioRepository.SaveChangesAsync(cancellationToken);

            // monta o response já com os novos campos
            return new UsuarioResponseDto
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
                IsAdmin = usuario.IsAdmin,

                NomeFantasia = usuario.NomeFantasia,
                RazaoSocial = usuario.RazaoSocial,
                Site = usuario.Site,

                EnderecoCep = usuario.EnderecoCep,
                EnderecoLogradouro = usuario.EnderecoLogradouro,
                EnderecoNumero = usuario.EnderecoNumero,
                EnderecoComplemento = usuario.EnderecoComplemento,
                EnderecoBairro = usuario.EnderecoBairro,
                EnderecoCidade = usuario.EnderecoCidade,
                EnderecoUF = usuario.EnderecoUF,

                NomeCompletoBanco = usuario.NomeCompletoBanco,
                CpfCnpjDadosBancarios = usuario.CpfCnpjDadosBancarios,
                CpfCnpjDadosBancariosFormatado = DocumentoFormatter.Mask(usuario.CpfCnpjDadosBancarios),
                ChavePix = usuario.ChavePix,
                ChaveCarteiraCripto = usuario.ChaveCarteiraCripto
            };
        }

        public async Task<string> ResetSenhaRequestAsync(SenhaEsquecidaRequest senhaEsquecidaRequest, string baseResetUrl, string? ip, string? userAgent, CancellationToken cancellationToken)
        {
            var email = senhaEsquecidaRequest.Email.Trim().ToLowerInvariant();
            var user = await usuarioRepository.GetByEmailAsync(email, cancellationToken);
            if (user is null) return "";

            await novaSenhaRepository.InvalidateUserTokensAsync(user.Id, cancellationToken);

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
            }, cancellationToken);

            await novaSenhaRepository.SaveChangesAsync(cancellationToken);

            var link = $"{baseResetUrl}?token={Uri.EscapeDataString(raw)}";

            await emailEnvio.EnvioResetSenhaAsync(user.Email, user.Nome, link, cancellationToken);

            return link;
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
    }
}
