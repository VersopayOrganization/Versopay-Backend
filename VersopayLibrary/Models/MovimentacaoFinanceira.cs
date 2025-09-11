using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static VersopayLibrary.Enums.FinanceiroEnums;

namespace VersopayLibrary.Models
{
    public class MovimentacaoFinanceira
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();

        [Required] public int ClienteId { get; set; }
        public Usuario Cliente { get; set; } = default!;

        [Required] public TipoMovimentacao Tipo { get; set; }
        [Required] public StatusMovimentacao Status { get; set; } = StatusMovimentacao.Pendente;

        [Required] public decimal Valor { get; set; }

        [MaxLength(200)] public string? Descricao { get; set; }
        [MaxLength(80)] public string? Referencia { get; set; } // ex: pedido, antecipação etc.

        public DateTime CriadoEmUtc { get; set; } = DateTime.UtcNow;
        public DateTime? EfetivadoEmUtc { get; set; }
        public DateTime? CanceladoEmUtc { get; set; }
    }
}
