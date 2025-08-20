using System.ComponentModel.DataAnnotations;

namespace VersopayBackend.Dtos
{
    public class PedidoCreateDto
    {
        [Required] public int VendedorId { get; set; }
        [Required, Range(0.01, double.MaxValue)] public decimal Valor { get; set; }

        // Envie "Pix", "Boleto", "Cartao" (case-insensitive)
        [Required, MaxLength(32)] public string MetodoPagamento { get; set; } = default!;

        [MaxLength(200)] public string? Produto { get; set; }
    }
}
