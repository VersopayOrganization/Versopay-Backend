using System.ComponentModel.DataAnnotations;
using VersopayLibrary.Models;
using Tipo = VersopayLibrary.Models.TipoCadastro; // << alias p/ o TIPO, evita colisão


namespace VersopayBackend.Dtos
{
    public class UsuarioCompletarCadastroDto : IValidatableObject
    {
        public int Id { get; set; }

        [Required]
        public Tipo TipoCadastro { get; set; }

        [Required, MaxLength(20)]
        public string? CpfCnpj { get; set; }

        [MaxLength(80)]
        public string? Instagram { get; set; }

        [MaxLength(20)]
        public string? Telefone { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext _)
        {
            var digits = new string((CpfCnpj ?? "").Where(char.IsDigit).ToArray());
            if (TipoCadastro == Tipo.PF && digits.Length != 11)
                yield return new("CPF deve ter 11 dígitos.", new[] { nameof(CpfCnpj) });
            if (TipoCadastro == Tipo.PJ && digits.Length != 14)
                yield return new("CNPJ deve ter 14 dígitos.", new[] { nameof(CpfCnpj) });
        }
    }
}