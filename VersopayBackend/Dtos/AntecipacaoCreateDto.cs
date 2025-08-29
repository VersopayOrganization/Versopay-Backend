using System.ComponentModel.DataAnnotations;
using VersopayLibrary.Enums;

namespace VersopayBackend.Dtos
{
    public class AntecipacaoCreateDto : IValidatableObject
    {
        [Required]
        public int EmpresaId { get; set; }

        [Required]
        public decimal Valor { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext _)
        {
            if (Valor <= 0m)
                yield return new ValidationResult("Valor deve ser maior que zero.", new[] { nameof(Valor) });
        }
    }
}
