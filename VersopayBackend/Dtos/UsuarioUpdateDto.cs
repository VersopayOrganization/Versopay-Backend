using System.ComponentModel.DataAnnotations;
using VersopayLibrary.Models;

namespace VersopayBackend.Dtos
{
    public class UsuarioUpdateDto : IValidatableObject
    {
        [Required, MaxLength(120)] public string Nome { get; set; } = default!;
        [Required] public TipoCadastro TipoCadastro { get; set; }
        [Required] public string? CpfCnpj { get; set; } = default!;
        [MaxLength(80)] public string? Instagram { get; set; }
        [MaxLength(20)] public string? Telefone { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext _)
        {
            var digits = new string((CpfCnpj ?? "").Where(char.IsDigit).ToArray());
            if (TipoCadastro == TipoCadastro.PF && digits.Length != 11)
                yield return new("CPF deve ter 11 dígitos.", new[] { nameof(CpfCnpj) });
            if (TipoCadastro == TipoCadastro.PJ && digits.Length != 14)
                yield return new("CNPJ deve ter 14 dígitos.", new[] { nameof(CpfCnpj) });
        }
    }
}
