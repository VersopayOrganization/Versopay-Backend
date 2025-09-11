using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersopayLibrary.Models
{
    public class Extrato
    {
        public int Id { get; set; }

        [Required] public int ClienteId { get; set; }
        public Usuario Cliente { get; set; } = default!;

        public decimal SaldoDisponivel { get; set; }
        public decimal SaldoPendente { get; set; }
        public decimal ReservaFinanceira { get; set; }

        public DateTime AtualizadoEmUtc { get; set; } = DateTime.UtcNow;
    }
}
