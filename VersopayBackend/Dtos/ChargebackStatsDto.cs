namespace VersopayBackend.Dtos
{
    public sealed class ChargebackStatsDto
    {
        public decimal Percent { get; set; }                  // estornos/ (aprovados?) * 100
        public int Qtde { get; set; }
        public decimal Total { get; set; }
    }
}
