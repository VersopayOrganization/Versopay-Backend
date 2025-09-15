namespace VersopayBackend.Dtos
{
    public sealed class DashboardResumoDto
    {
        public decimal FaturamentoPeriodo { get; set; }  // ex.: mês atual
        public decimal SaldoDisponivel { get; set; }
        public decimal SaldoPendente { get; set; }

        public MetodoAprovacaoDto Cartao { get; set; } = new();
        public MetodoAprovacaoDto Pix { get; set; } = new();
        public MetodoAprovacaoDto Boleto { get; set; } = new();
        public ChargebackResumoDto Chargeback { get; set; } = new();
        public DateTime PeriodoDeUtc { get; set; }
        public DateTime PeriodoAteUtc { get; set; }
    }
}
