using static VersopayLibrary.Enums.FinanceiroEnums;

namespace VersopayBackend.Dtos
{
    public sealed class MovimentacaoResponseDto
    {
        public Guid Id { get; set; }
        public int ClienteId { get; set; }
        public TipoMovimentacao Tipo { get; set; }
        public StatusMovimentacao Status { get; set; }
        public decimal Valor { get; set; }
        public string? Descricao { get; set; }
        public string? Referencia { get; set; }
        public DateTime CriadoEmUtc { get; set; }
        public DateTime? EfetivadoEmUtc { get; set; }
        public DateTime? CanceladoEmUtc { get; set; }
    }
}
