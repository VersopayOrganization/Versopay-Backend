namespace VersopayBackend.Dtos
{
    public sealed class DashboardResponseDto
    {
        public decimal Faturamento { get; set; }              // total aprovado no período
        public decimal SaldoDisponivel { get; set; }
        public decimal SaldoPendente { get; set; }

        public MetodoStatsDto Cartao { get; set; } = new();
        public MetodoStatsDto Pix { get; set; } = new();
        public MetodoStatsDto Boleto { get; set; } = new();

        public ChargebackStatsDto Chargeback { get; set; } = new();
    }
}
