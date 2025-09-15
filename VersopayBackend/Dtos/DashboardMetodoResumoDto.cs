namespace VersopayBackend.Dtos
{
    public sealed class DashboardMetodoResumoDto
    {
        public decimal PercentualAprovacao { get; set; } // 0–100
        public int QtdPedidosAprovados { get; set; }
        public decimal TotalAprovado { get; set; }
        public int QtdPedidosTotal { get; set; }
        public decimal TotalGeral { get; set; }
    }
}
