using System.ComponentModel.DataAnnotations;

namespace VersopayLibrary.Models
{
    public class Faturamento
    {
        public int Id { get; set; }

        [MaxLength(11)] 
        public string? Cpf { get; set; }   // só dígitos
        [MaxLength(14)] 
        public string? Cnpj { get; set; }  // só dígitos

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
