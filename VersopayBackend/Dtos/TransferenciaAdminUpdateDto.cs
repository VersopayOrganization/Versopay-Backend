using System.ComponentModel.DataAnnotations;
using VersopayLibrary.Enums;

namespace VersopayBackend.Dtos
{
    public sealed class TransferenciaAdminUpdateDto
    {
        [Required] public StatusTransferencia Status { get; set; }
        [Required] public AprovacaoManual Aprovacao { get; set; } = AprovacaoManual.Pendente;
        public TipoEnvioManual? TipoEnvio { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Taxa { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? ValorFinal { get; set; }
    }
}
