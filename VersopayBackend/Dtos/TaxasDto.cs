namespace VersopayBackend.Dtos
{
    public sealed class TaxasDto
    {
        // exemplo: 3.99m => 3,99%
        public decimal CartaoPorcentagem { get; set; }
        public decimal PixEntradaPorcentagem { get; set; }
        public decimal PixSaidaPorcentagem { get; set; }
        public decimal PixSaidaFixo { get; set; }     // R$
        public decimal BoletoPorcentagem { get; set; }
        public decimal BoletoFixo { get; set; }       // R$
    }
}
