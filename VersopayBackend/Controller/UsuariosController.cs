using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VersopayBackend.Dtos;
using VersopayDatabase.Data;
using VersopayLibrary.Models;

[ApiController]
[Route("api/[controller]")]
public class UsuariosController(AppDbContext db) : ControllerBase
{
    static string Digits(string? s) => string.IsNullOrWhiteSpace(s) ? "" : new string(s.Where(char.IsDigit).ToArray());
    static string? MaskDocumento(string? d)
    {
        if (string.IsNullOrWhiteSpace(d)) return null;
        var x = new string(d.Where(char.IsDigit).ToArray());
        if (x.Length == 11) return Convert.ToUInt64(x).ToString(@"000\.000\.000\-00");
        if (x.Length == 14) return Convert.ToUInt64(x).ToString(@"00\.000\.000\/0000\-00");
        return x;
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioResponseDto>> Create([FromBody] UsuarioCreateDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await db.Usuarios.AnyAsync(u => u.Email == email))
            return Conflict(new { message = "Email já cadastrado." });

        var u = new Usuario
        {
            Nome = dto.Nome.Trim(),
            Email = email,
            TipoCadastro = dto.TipoCadastro,
            CpfCnpj = Digits(dto.CpfCnpj),
            Instagram = string.IsNullOrWhiteSpace(dto.Instagram) ? null :
                        (dto.Instagram.StartsWith("@") ? dto.Instagram.Trim() : "@" + dto.Instagram.Trim()),
            Telefone = dto.Telefone
        };

        var hasher = new PasswordHasher<Usuario>();
        u.SenhaHash = hasher.HashPassword(u, dto.Senha);

        db.Usuarios.Add(u);
        await db.SaveChangesAsync();

        var resp = new UsuarioResponseDto
        {
            Id = u.Id,
            Nome = u.Nome,
            Email = u.Email,
            TipoCadastro = u.TipoCadastro,
            Instagram = u.Instagram,
            Telefone = u.Telefone,
            CreatedAt = u.DataCriacao,
            CpfCnpj = u.CpfCnpj,
            CpfCnpjFormatado = MaskDocumento(u.CpfCnpj)
        };
        return CreatedAtAction(nameof(GetById), new { id = u.Id }, resp);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UsuarioResponseDto>>> GetAll()
    {
        var list = await db.Usuarios.AsNoTracking()
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
                CpfCnpj = u.CpfCnpj
            })
            .ToListAsync();

        foreach (var i in list) i.CpfCnpjFormatado = MaskDocumento(i.CpfCnpj);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UsuarioResponseDto>> GetById(int id)
    {
        var dto = await db.Usuarios.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UsuarioResponseDto
            {
                Id = u.Id,
                Nome = u.Nome,
                Email = u.Email,
                TipoCadastro = u.TipoCadastro,
                Instagram = u.Instagram,
                Telefone = u.Telefone,
                CreatedAt = u.DataCriacao,
                CpfCnpj = u.CpfCnpj
            })
            .FirstOrDefaultAsync();

        if (dto is null) return NotFound();
        dto.CpfCnpjFormatado = MaskDocumento(dto.CpfCnpj);
        return Ok(dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UsuarioResponseDto>> Update(Guid id, [FromBody] UsuarioUpdateDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var u = await db.Usuarios.FindAsync(id);
        if (u is null) return NotFound();

        u.Nome = dto.Nome.Trim();
        u.TipoCadastro = dto.TipoCadastro;
        u.CpfCnpj = Digits(dto.CpfCnpj);
        u.Instagram = string.IsNullOrWhiteSpace(dto.Instagram) ? null :
                      (dto.Instagram.StartsWith("@") ? dto.Instagram.Trim() : "@" + dto.Instagram.Trim());
        u.Telefone = dto.Telefone;
        u.DataAtualizacao = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Ok(new UsuarioResponseDto
        {
            Id = u.Id,
            Nome = u.Nome,
            Email = u.Email,
            TipoCadastro = u.TipoCadastro,
            Instagram = u.Instagram,
            Telefone = u.Telefone,
            CreatedAt = u.DataCriacao,
            CpfCnpj = u.CpfCnpj,
            CpfCnpjFormatado = MaskDocumento(u.CpfCnpj)
        });
    }
}
