namespace VersopayBackend.Dtos
{
    public sealed class ChargebackResumoDto
    {
        public int Qtd { get; set; }
        public decimal Total { get; set; }
        public decimal PercentualSobreTotalPedidos { get; set; } // 0..100 (no período)
    }
}
