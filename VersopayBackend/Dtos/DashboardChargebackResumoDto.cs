namespace VersopayBackend.Dtos
{
    public sealed class DashboardChargebackResumoDto
    {
        public decimal Percentual { get; set; } // 0–100
        public int Qtd { get; set; }
        public decimal Total { get; set; }
    }
}
