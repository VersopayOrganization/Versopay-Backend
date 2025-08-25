using Microsoft.AspNetCore.Identity;
using VersopayBackend.Auth;
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
                Email = email
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

        public async Task<UsuarioResponseDto?> UpdateAsync(int id, UsuarioUpdateDto usuarioResposedto, CancellationToken cancellationToken)
        {
            var usuario = await usuarioRepository.FindByIdAsync(id, cancellationToken);
            if (usuario is null) return null;

            var digits = CpfCnpjUtils.ValidarQtdDigitos(usuarioResposedto.CpfCnpj);
            if (!CpfCnpjUtils.IsValidoTipo(digits, usuarioResposedto.TipoCadastro))
                throw new ArgumentException("CpfCnpj não condiz com o TipoCadastro.");

            if (!string.Equals(usuario.CpfCnpj, digits, StringComparison.Ordinal) &&
                await usuarioRepository.CpfCnpjExistsAsync(digits, cancellationToken))
                throw new InvalidOperationException("CPF/CNPJ já cadastrado.");

            usuario.Nome = usuarioResposedto.Nome.Trim();
            usuario.TipoCadastro = usuarioResposedto.TipoCadastro;
            usuario.CpfCnpj = digits;
            usuario.Instagram = string.IsNullOrWhiteSpace(usuarioResposedto.Instagram) ? null :
                          (usuarioResposedto.Instagram.StartsWith("@") ? usuarioResposedto.Instagram.Trim() : "@" + usuarioResposedto.Instagram.Trim());
            usuario.Telefone = usuarioResposedto.Telefone;
            usuario.DataAtualizacao = DateTime.UtcNow;

            await usuarioRepository.SaveChangesAsync(cancellationToken);

            var response = usuario.ToResponseDto();
            response.CpfCnpjFormatado = CpfCnpjUtils.Mascara(response.CpfCnpj);
            return response;
        }

        public async Task<string> ResetSenhaRequestAsync(SenhaEsquecidaRequest senhaEsquecidaRequest, string baseResetUrl, string ip, string userAgent, CancellationToken cancellationToken)
        {
            var email = senhaEsquecidaRequest.Email.Trim().ToLowerInvariant();
            var user = await usuarioRepository.GetByEmailAsync(email, cancellationToken);

            if (user is null) return "";

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

            //TODO: QUANDO TIVERMOS O ENVIO POR EMAIL, NAO PODEMOS RETORNAR O LINK NO ENDPOINT! Deverá ser removido essa linha de baixo
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
