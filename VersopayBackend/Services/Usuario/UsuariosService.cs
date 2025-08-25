using Microsoft.AspNetCore.Identity;
using VersopayBackend.Dtos;
using VersopayBackend.Repositories;
using VersopayBackend.Utils;
using VersopayLibrary.Models;

namespace VersopayBackend.Services
{
    public sealed class UsuariosService(IUsuarioRepository usuarioRepository) : IUsuariosService
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
                Instagram = null,
                Telefone = null
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
    }
}
