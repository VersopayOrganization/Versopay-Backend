using System.ComponentModel.DataAnnotations;

namespace VersopayLibrary.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required, MaxLength(120)]
        public string Nome { get; set; } = default!;

        [Required, EmailAddress, MaxLength(160)]
        public string Email { get; set; } = default!;

        [Required]
        public string SenhaHash { get; set; } = default!;

        public TipoCadastro? TipoCadastro { get; set; }

        // CPF (11) — armazene apenas dígitos
        [MaxLength(11)]
        public string? Cpf { get; set; }

        // CNPJ (14) — armazene apenas dígitos
        [MaxLength(14)]
        public string? Cnpj { get; set; }

        [MaxLength(80)]
        public string? Instagram { get; set; }

        [MaxLength(20)]
        public string? Telefone { get; set; }
        public bool IsAdmin { get; set; } = false;


        [MaxLength(160)] public string? NomeFantasia { get; set; }
        [MaxLength(160)] public string? RazaoSocial { get; set; }
        [MaxLength(160)] public string? Site { get; set; }             // pode guardar URL

        // Endereço
        [MaxLength(9)] public string? EnderecoCep { get; set; }       // você pode salvar só dígitos; ver service
        [MaxLength(120)] public string? EnderecoLogradouro { get; set; }
        [MaxLength(20)] public string? EnderecoNumero { get; set; }
        [MaxLength(80)] public string? EnderecoComplemento { get; set; }
        [MaxLength(80)] public string? EnderecoBairro { get; set; }
        [MaxLength(80)] public string? EnderecoCidade { get; set; }
        [MaxLength(2)] public string? EnderecoUF { get; set; }        // “RJ”, “SP”, …

        // Financeiro
        [MaxLength(160)] public string? NomeCompletoBanco { get; set; }
        [MaxLength(14)] public string? CpfCnpjDadosBancarios { get; set; }
        [MaxLength(120)] public string? ChavePix { get; set; }          // e-mail, aleatória, cpf/cnpj, etc
        [MaxLength(120)] public string? ChaveCarteiraCripto { get; set; }

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
        public DateTime? DataAtualizacao { get; set; }

        // 1:1
        public Documento? Documento { get; set; }

        public bool CadastroCompleto { get; set; } = false;
    }
}
