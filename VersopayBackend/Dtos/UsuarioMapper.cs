using VersopayBackend.Common;
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
            CpfCnpjFormatado = DocumentoFormatter.Mask(u.CpfCnpj),

            CpfCnpjDadosBancarios = u.CpfCnpjDadosBancarios,
            CpfCnpjDadosBancariosFormatado = DocumentoFormatter.Mask(u.CpfCnpjDadosBancarios),

            IsAdmin = u.IsAdmin,

            // ... (fantasia, endereço, banco etc.)
            NomeFantasia = u.NomeFantasia,
            RazaoSocial = u.RazaoSocial,
            Site = u.Site,
            EnderecoCep = u.EnderecoCep,
            EnderecoLogradouro = u.EnderecoLogradouro,
            EnderecoNumero = u.EnderecoNumero,
            EnderecoComplemento = u.EnderecoComplemento,
            EnderecoBairro = u.EnderecoBairro,
            EnderecoCidade = u.EnderecoCidade,
            EnderecoUF = u.EnderecoUF,
            NomeCompletoBanco = u.NomeCompletoBanco,
            ChavePix = u.ChavePix,
            ChaveCarteiraCripto = u.ChaveCarteiraCripto
        };
    }
}
