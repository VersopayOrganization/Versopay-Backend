namespace VersopayBackend.Dtos
{
    public sealed class ExtratoResponseDto
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public decimal SaldoDisponivel { get; set; }
        public decimal SaldoPendente { get; set; }
        public decimal ReservaFinanceira { get; set; }
        public DateTime AtualizadoEmUtc { get; set; }
    }
}
