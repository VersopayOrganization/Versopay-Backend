using Microsoft.AspNetCore.Identity;
using VersopayBackend.Dtos;
using VersopayBackend.Repositories;
using VersopayBackend.Utils;
using VersopayLibrary.Models;

namespace VersopayBackend.Services
{
    public sealed class UsuariosService(IUsuarioRepository usuarioRepository) : IUsuariosService
    {
        public async Task<UsuarioResponseDto> CreateAsync(UsuarioCreateDto usuarioCreatedto, CancellationToken cancellationToken)
        {
            var email = usuarioCreatedto.Email.Trim().ToLowerInvariant();

            if (await usuarioRepository.EmailExistsAsync(email, cancellationToken))
                throw new InvalidOperationException("Email já cadastrado.");

            var digits = CpfCnpjUtils.Digits(usuarioCreatedto.CpfCnpj);
            if (!CpfCnpjUtils.IsValidForTipo(digits, usuarioCreatedto.TipoCadastro))
                throw new ArgumentException("CpfCnpj não condiz com o TipoCadastro.");

            if (await usuarioRepository.CpfCnpjExistsAsync(digits, cancellationToken))
                throw new InvalidOperationException("CPF/CNPJ já cadastrado.");

            var u = new Usuario
            {
                Nome = usuarioCreatedto.Nome.Trim(),
                Email = email,
                TipoCadastro = usuarioCreatedto.TipoCadastro,
                CpfCnpj = digits,
                Instagram = string.IsNullOrWhiteSpace(usuarioCreatedto.Instagram) ? null :
                            (usuarioCreatedto.Instagram.StartsWith("@") ? usuarioCreatedto.Instagram.Trim() : "@" + usuarioCreatedto.Instagram.Trim()),
                Telefone = usuarioCreatedto.Telefone
            };

            var hasher = new PasswordHasher<Usuario>();
            u.SenhaHash = hasher.HashPassword(u, usuarioCreatedto.Senha);

            await usuarioRepository.AddAsync(u, cancellationToken);
            await usuarioRepository.SaveChangesAsync(cancellationToken);

            return u.ToResponseDto();
        }

        public async Task<IEnumerable<UsuarioResponseDto>> GetAllAsync(CancellationToken cancellationToken)
        {
            var usuarios = await usuarioRepository.GetAllNoTrackingAsync(cancellationToken);
            var usuariosLista = usuarios.Select(usuario => usuario.ToResponseDto()).ToList();
            foreach (var usuario in usuariosLista) usuario.CpfCnpjFormatado = CpfCnpjUtils.Mask(usuario.CpfCnpj);
            return usuariosLista;
        }

        public async Task<UsuarioResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            var usuario = await usuarioRepository.GetByIdNoTrackingAsync(id, cancellationToken);
            if (usuario is null) return null;

            var usuarioResposedto = usuario.ToResponseDto();
            usuarioResposedto.CpfCnpjFormatado = CpfCnpjUtils.Mask(usuarioResposedto.CpfCnpj);
            return usuarioResposedto;
        }

        public async Task<UsuarioResponseDto?> UpdateAsync(int id, UsuarioUpdateDto usuarioResposedto, CancellationToken cancellationToken)
        {
            var usuario = await usuarioRepository.FindByIdAsync(id, cancellationToken);
            if (usuario is null) return null;

            var digits = CpfCnpjUtils.Digits(usuarioResposedto.CpfCnpj);
            if (!CpfCnpjUtils.IsValidForTipo(digits, usuarioResposedto.TipoCadastro))
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
            response.CpfCnpjFormatado = CpfCnpjUtils.Mask(response.CpfCnpj);
            return response;
        }
    }
}
