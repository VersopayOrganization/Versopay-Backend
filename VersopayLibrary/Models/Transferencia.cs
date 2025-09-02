using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersopayLibrary.Enums;

namespace VersopayLibrary.Models
{
    public class Transferencia
    {
        public int Id { get; set; }

        [Required]
        public int SolicitanteId { get; set; }
        public Usuario Solicitante { get; set; } = default!;

        [Required]
        public StatusTransferencia Status { get; set; } = StatusTransferencia.PendenteAnalise;

        // Lista/grade
        public DateTime DataSolicitacao { get; set; } = DateTime.UtcNow;
        [Required]
        public decimal ValorSolicitado { get; set; }

        // Detalhes (snapshots)
        [MaxLength(120)]
        public string? Nome { get; set; }  // snapshot do solicitante

        [MaxLength(160)]
        public string? Empresa { get; set; }  // se PJ; snapshot

        [MaxLength(120)]
        public string? ChavePix { get; set; }

        [Required]
        public AprovacaoManual Aprovacao { get; set; } = AprovacaoManual.Pendente;

        public TipoEnvioManual? TipoEnvio { get; set; }

        public decimal? Taxa { get; set; }
        public decimal? ValorFinal { get; set; }

        // Auditoria
        public DateTime DataCadastro { get; set; } = DateTime.UtcNow; 
        public DateTime? DataAprovacao { get; set; }                  
    }
}
