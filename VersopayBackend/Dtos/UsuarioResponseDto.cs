using VersopayLibrary.Models;

namespace VersopayBackend.Dtos
{
    public sealed class UsuarioResponseDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = default!;
        public string Email { get; set; } = default!;
        public TipoCadastro? TipoCadastro { get; set; }
        public string? Instagram { get; set; }
        public string? Telefone { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CpfCnpj { get; set; }
        public string? CpfCnpjFormatado { get; set; }
        public bool IsAdmin { get; set; }
        public string? NomeFantasia { get; set; }
        public string? RazaoSocial { get; set; }
        public string? Site { get; set; }
        public string? EnderecoCep { get; set; }
        public string? EnderecoLogradouro { get; set; }
        public string? EnderecoNumero { get; set; }
        public string? EnderecoComplemento { get; set; }
        public string? EnderecoBairro { get; set; }
        public string? EnderecoCidade { get; set; }
        public string? EnderecoUF { get; set; }
        public string? NomeCompletoBanco { get; set; }
        public string? CpfCnpjDadosBancarios { get; set; }
        public string? CpfCnpjDadosBancariosFormatado { get; set; }
        public string? ChavePix { get; set; }
        public string? ChaveCarteiraCripto { get; set; }
        public bool CadastroCompleto { get; set; }
    }
}