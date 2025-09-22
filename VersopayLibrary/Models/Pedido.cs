using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersopayLibrary.Enums;

namespace VersopayLibrary.Models
{
    public class Pedido
    {
        public int Id { get; set; }

        public DateTime Criacao { get; set; } = DateTime.UtcNow;
        public DateTime? DataPagamento { get; set; }

        public MetodoPagamento MetodoPagamento { get; set; }

        // sempre decimal p/ dinheiro
        public decimal Valor { get; set; }

        // FK -> Usuario (vendedor)
        public int VendedorId { get; set; }
        public Usuario Vendedor { get; set; } = default!;

        public string? Produto { get; set; }

        public StatusPedido Status { get; set; } = StatusPedido.Pendente;

        public string? ExternalId { get; set; }             // seu id/controle (idempotência)
        public string? GatewayTransactionId { get; set; }   // id que vem do provedor (para casar no webhook)
    }
}
