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

        // CPF (11) ou CNPJ (14) — armazene apenas dígitos
        [MaxLength(14)]
        public string? CpfCnpj { get; set; }

        [MaxLength(80)]
        public string? Instagram { get; set; }

        [MaxLength(20)]
        public string? Telefone { get; set; }
        public bool IsAdmin { get; set; } = false;

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
        public DateTime? DataAtualizacao { get; set; }

        // 1:1
        public Documento? Documento { get; set; }
    }
}
