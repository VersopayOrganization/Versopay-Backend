using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Dtos
{
    public class PedidoDto
    {
        public int Id { get; set; }

        // já existia: UTC (com Z)
        public DateTime Criacao { get; set; }

        // novo: horário de Brasília com offset -03:00
        public DateTimeOffset CriacaoBr { get; set; }

        public DateTime? DataPagamento { get; set; }
        public string MetodoPagamento { get; set; } = default!;
        public decimal Valor { get; set; }
        public int VendedorId { get; set; }
        public string? VendedorNome { get; set; }
        public string? Produto { get; set; }
        public StatusPedido Status { get; set; }
    }
}
