using System.ComponentModel.DataAnnotations;
using static VersopayLibrary.Enums.FinanceiroEnums;

namespace VersopayBackend.Dtos
{
    public sealed class MovimentacaoCreateDto
    {
        [Required] public int ClienteId { get; set; }
        [Required] public TipoMovimentacao Tipo { get; set; }
        [Required, Range(0.01, double.MaxValue)] public decimal Valor { get; set; }
        [MaxLength(200)] public string? Descricao { get; set; }
        [MaxLength(80)] public string? Referencia { get; set; }
        public bool EfetivarAgora { get; set; } = false;
    }
}
