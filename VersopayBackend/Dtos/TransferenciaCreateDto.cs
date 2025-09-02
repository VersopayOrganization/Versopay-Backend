using System.ComponentModel.DataAnnotations;

namespace VersopayBackend.Dtos
{
    public sealed class TransferenciaCreateDto
    {
        [Required] public int SolicitanteId { get; set; }

        [Required, Range(0.01, double.MaxValue)]
        public decimal ValorSolicitado { get; set; }

        [MaxLength(120)]
        public string? ChavePix { get; set; }
    }
}
