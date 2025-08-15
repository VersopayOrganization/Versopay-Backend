using System.ComponentModel.DataAnnotations;

namespace VersopayLibrary.Models
{
    public class Usuario
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(120)]
        public string Nome { get; set; } = default!;

        [Required, EmailAddress, MaxLength(160)]
        public string Email { get; set; } = default!;

        [Required]
        public string SenhaHash { get; set; } = default!;

        [Required]
        public TipoCadastro TipoCadastro { get; set; }

        // CPF (11) ou CNPJ (14) — armazene apenas dígitos
        [Required, MaxLength(14)]
        public string CpfCnpj { get; set; } = default!;

        [MaxLength(80)]
        public string? Instagram { get; set; }

        [MaxLength(20)]
        public string? Telefone { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // 1:1
        public Documento? Documento { get; set; }
    }
}
