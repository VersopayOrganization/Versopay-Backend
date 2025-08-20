using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Dtos
{
    public class PedidoResponseDto
    {
        public int Id { get; set; }
        public DateTime Criacao { get; set; }
        public DateTime? DataPagamento { get; set; }

        public string MetodoPagamento { get; set; } = default!;
        public decimal Valor { get; set; }

        public int VendedorId { get; set; }
        public string? VendedorNome { get; set; }

        public string? Produto { get; set; }
        public StatusPedido Status { get; set; }
    }
}
