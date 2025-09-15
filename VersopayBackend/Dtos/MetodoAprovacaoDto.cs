namespace VersopayBackend.Dtos
{
    public sealed class MetodoAprovacaoDto
    {
        public int QtdTotal { get; set; }
        public int QtdAprovado { get; set; }
        public decimal Total { get; set; }
        public decimal TotalAprovado { get; set; }
        public decimal PercentAprovacao { get; set; }     // 0..100
    }
}
