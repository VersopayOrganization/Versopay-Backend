using System.ComponentModel.DataAnnotations;

namespace VersopayLibrary.Models
{
    public class Faturamento
    {
        public int Id { get; set; }

        [Required, MaxLength(14)]
        public string CpfCnpj { get; set; } = default!; // apenas dígitos (11 ou 14)

        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }

        public decimal VendasTotais { get; set; }
        public decimal VendasCartao { get; set; }
        public decimal VendasBoleto { get; set; }
        public decimal VendasPix { get; set; }
        public decimal Reserva { get; set; }

        public int VendasCanceladas { get; set; }
        public int DiasSemVendas { get; set; }

        public DateTime AtualizadoEmUtc { get; set; } = DateTime.UtcNow;
    }
}
