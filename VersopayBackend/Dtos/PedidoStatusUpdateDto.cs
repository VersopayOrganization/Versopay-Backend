using System.ComponentModel.DataAnnotations;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Dtos
{
    public class PedidoStatusUpdateDto
    {
        [Required] public StatusPedido Status { get; set; }
    }
}
