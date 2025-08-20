using Microsoft.AspNetCore.Identity;
using VersopayBackend.Dtos;
using VersopayBackend.Repositories;
using VersopayBackend.Utils;
using VersopayLibrary.Models;

namespace VersopayBackend.Services
{
    public sealed class UsuariosService(IUsuarioRepository usuarioRepository) : IUsuariosService
    {
        public async Task<UsuarioResponseDto> CreateAsync(UsuarioCreateDto dto, CancellationToken ct)
        {
            var email = dto.Email.Trim().ToLowerInvariant();

            if (await usuarioRepository.EmailExistsAsync(email, ct))
                throw new InvalidOperationException("Email já cadastrado.");

            var digits = CpfCnpjUtils.Digits(dto.CpfCnpj);
            if (!CpfCnpjUtils.IsValidForTipo(digits, dto.TipoCadastro))
                throw new ArgumentException("CpfCnpj não condiz com o TipoCadastro.");

            if (await usuarioRepository.CpfCnpjExistsAsync(digits, ct))
                throw new InvalidOperationException("CPF/CNPJ já cadastrado.");

            var u = new Usuario
            {
                Nome = dto.Nome.Trim(),
                Email = email,
                TipoCadastro = dto.TipoCadastro,
                CpfCnpj = digits,
                Instagram = string.IsNullOrWhiteSpace(dto.Instagram) ? null :
                            (dto.Instagram.StartsWith("@") ? dto.Instagram.Trim() : "@" + dto.Instagram.Trim()),
                Telefone = dto.Telefone
            };

            var hasher = new PasswordHasher<Usuario>();
            u.SenhaHash = hasher.HashPassword(u, dto.Senha);

            await usuarioRepository.AddAsync(u, ct);
            await usuarioRepository.SaveChangesAsync(ct);

            return u.ToResponseDto();
        }

        public async Task<IEnumerable<UsuarioResponseDto>> GetAllAsync(CancellationToken ct)
        {
            var usuarios = await usuarioRepository.GetAllNoTrackingAsync(ct);
            var list = usuarios.Select(u => u.ToResponseDto()).ToList();
            foreach (var i in list) i.CpfCnpjFormatado = CpfCnpjUtils.Mask(i.CpfCnpj);
            return list;
        }

        public async Task<UsuarioResponseDto?> GetByIdAsync(int id, CancellationToken ct)
        {
            var u = await usuarioRepository.GetByIdNoTrackingAsync(id, ct);
            if (u is null) return null;

            var dto = u.ToResponseDto();
            dto.CpfCnpjFormatado = CpfCnpjUtils.Mask(dto.CpfCnpj);
            return dto;
        }

        public async Task<UsuarioResponseDto?> UpdateAsync(int id, UsuarioUpdateDto dto, CancellationToken ct)
        {
            var u = await usuarioRepository.FindByIdAsync(id, ct);
            if (u is null) return null;

            var digits = CpfCnpjUtils.Digits(dto.CpfCnpj);
            if (!CpfCnpjUtils.IsValidForTipo(digits, dto.TipoCadastro))
                throw new ArgumentException("CpfCnpj não condiz com o TipoCadastro.");

            if (!string.Equals(u.CpfCnpj, digits, StringComparison.Ordinal) &&
                await usuarioRepository.CpfCnpjExistsAsync(digits, ct))
                throw new InvalidOperationException("CPF/CNPJ já cadastrado.");

            u.Nome = dto.Nome.Trim();
            u.TipoCadastro = dto.TipoCadastro;
            u.CpfCnpj = digits;
            u.Instagram = string.IsNullOrWhiteSpace(dto.Instagram) ? null :
                          (dto.Instagram.StartsWith("@") ? dto.Instagram.Trim() : "@" + dto.Instagram.Trim());
            u.Telefone = dto.Telefone;
            u.DataAtualizacao = DateTime.UtcNow;

            await usuarioRepository.SaveChangesAsync(ct);

            var resp = u.ToResponseDto();
            resp.CpfCnpjFormatado = CpfCnpjUtils.Mask(resp.CpfCnpj);
            return resp;
        }
    }
}
