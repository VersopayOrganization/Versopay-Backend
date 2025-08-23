using System.ComponentModel.DataAnnotations;
using VersopayBackend.Utils;

namespace VersopayBackend.Dtos
{
    public class UsuarioCreateDto : IValidatableObject
    {
        [Required, MaxLength(120)]
        public string Nome { get; set; } = default!;

        [Required, EmailAddress, MaxLength(160)]
        public string Email { get; set; } = default!;

        [Required, MinLength(6)]
        public string Senha { get; set; } = default!;

        [Required, MinLength(6)]
        public string ConfirmarSenha { get; set; } = default!;

        public IEnumerable<ValidationResult> Validate(ValidationContext _)
        {
            if (!string.Equals(Senha, ConfirmarSenha, StringComparison.Ordinal))
                yield return new ValidationResult("Senha e confirmação não conferem.", new[] { nameof(ConfirmarSenha) });

             if (!ValidacaoPadraoSenha.IsValid(Senha)) yield return new ValidationResult("Regra de senha inválida");
        }
    }
}