using System.ComponentModel.DataAnnotations;
using Tipo = VersopayLibrary.Models.TipoCadastro;

namespace VersopayBackend.Dtos
{
    public class UsuarioCompletarCadastroDto : IValidatableObject
    {
        public int Id { get; set; }

        [Required]
        public Tipo TipoCadastro { get; set; }

        // IMPORTANTE: não marque ambos como [Required]; a validação condicional cuida disso.
        [MaxLength(11)]
        public string? Cpf { get; set; }

        [MaxLength(14)]
        public string? Cnpj { get; set; }

        [MaxLength(80)]
        public string? Instagram { get; set; }

        [MaxLength(20)]
        public string? Telefone { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext _)
        {
            if (TipoCadastro == Tipo.PF)
            {
                var cpfDigits = new string((Cpf ?? "").Where(char.IsDigit).ToArray());
                if (cpfDigits.Length != 11)
                    yield return new ValidationResult("CPF deve ter 11 dígitos.", new[] { nameof(Cpf) });
            }
            else if (TipoCadastro == Tipo.PJ)
            {
                var cnpjDigits = new string((Cnpj ?? "").Where(char.IsDigit).ToArray());
                if (cnpjDigits.Length != 14)
                    yield return new ValidationResult("CNPJ deve ter 14 dígitos.", new[] { nameof(Cnpj) });
            }
        }
    }
}
