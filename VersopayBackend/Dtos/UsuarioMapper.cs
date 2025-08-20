using VersopayLibrary.Models;

namespace VersopayBackend.Dtos
{
    public static class UsuarioMapper
    {
        public static UsuarioResponseDto ToResponseDto(this Usuario u) => new()
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
