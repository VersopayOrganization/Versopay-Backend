using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VersopayBackend.Dtos;
using VersopayBackend.Repositories;
using VersopayBackend.Utils;
using VersopayLibrary.Models;

namespace VersopayBackend.Services
{
    public sealed class UsuariosService(IUsuarioRepository repo) : IUsuariosService
    {
        public async Task<UsuarioResponseDto> CreateAsync(UsuarioCreateDto dto, CancellationToken ct)
        {
            var email = dto.Email.Trim().ToLowerInvariant();

            // Duplicidades
            if (await repo.EmailExistsAsync(email, ct))
                throw new InvalidOperationException("Email já cadastrado.");

            var digits = CpfCnpjUtils.Digits(dto.CpfCnpj);
            if (!CpfCnpjUtils.IsValidForTipo(digits, dto.TipoCadastro))
                throw new ArgumentException("CpfCnpj não condiz com o TipoCadastro.");

            if (await repo.CpfCnpjExistsAsync(digits, ct))
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

            await repo.AddAsync(u, ct);
            await repo.SaveChangesAsync(ct);

            return MapToResponse(u);
        }

        public async Task<IEnumerable<UsuarioResponseDto>> GetAllAsync(CancellationToken ct)
        {
            var q = repo.QueryNoTracking()
                        .OrderByDescending(u => u.DataCriacao)
                        .Select(u => new UsuarioResponseDto
                        {
                            Id = u.Id,
                            Nome = u.Nome,
                            Email = u.Email,
                            TipoCadastro = u.TipoCadastro,
                            Instagram = u.Instagram,
                            Telefone = u.Telefone,
                            CreatedAt = u.DataCriacao,
                            CpfCnpj = u.CpfCnpj,
                            IsAdmin = u.IsAdmin
                        });

            var list = await q.ToListAsync(ct);
            foreach (var i in list) i.CpfCnpjFormatado = CpfCnpjUtils.Mask(i.CpfCnpj);
            return list;
        }

        public async Task<UsuarioResponseDto?> GetByIdAsync(int id, CancellationToken ct)
        {
            var u = await repo.QueryNoTracking()
                              .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (u is null) return null;

            var dto = MapToResponse(u);
            dto.CpfCnpjFormatado = CpfCnpjUtils.Mask(dto.CpfCnpj);
            return dto;
        }

        public async Task<UsuarioResponseDto?> UpdateAsync(int id, UsuarioUpdateDto dto, CancellationToken ct)
        {
            var u = await repo.FindByIdAsync(id, ct);
            if (u is null) return null;

            var digits = CpfCnpjUtils.Digits(dto.CpfCnpj);
            if (!CpfCnpjUtils.IsValidForTipo(digits, dto.TipoCadastro))
                throw new ArgumentException("CpfCnpj não condiz com o TipoCadastro.");

            // se mudou o documento, garanta unicidade
            if (!string.Equals(u.CpfCnpj, digits, StringComparison.Ordinal) &&
                await repo.CpfCnpjExistsAsync(digits, ct))
                throw new InvalidOperationException("CPF/CNPJ já cadastrado.");

            u.Nome = dto.Nome.Trim();
            u.TipoCadastro = dto.TipoCadastro;
            u.CpfCnpj = digits;
            u.Instagram = string.IsNullOrWhiteSpace(dto.Instagram) ? null :
                          (dto.Instagram.StartsWith("@") ? dto.Instagram.Trim() : "@" + dto.Instagram.Trim());
            u.Telefone = dto.Telefone;
            u.DataAtualizacao = DateTime.UtcNow;

            await repo.SaveChangesAsync(ct);

            var resp = MapToResponse(u);
            resp.CpfCnpjFormatado = CpfCnpjUtils.Mask(resp.CpfCnpj);
            return resp;
        }

        private static UsuarioResponseDto MapToResponse(Usuario u) => new()
        {
            Id = u.Id,
            Nome = u.Nome,
            Email = u.Email,
            TipoCadastro = u.TipoCadastro,
            Instagram = u.Instagram,
            Telefone = u.Telefone,
            CreatedAt = u.DataCriacao,
            CpfCnpj = u.CpfCnpj,
            IsAdmin = u.IsAdmin
        };
    }
}
