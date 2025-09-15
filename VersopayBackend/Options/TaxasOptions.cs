namespace VersopayBackend.Options
{
    public sealed class TaxasOptions
    {
        public decimal CartaoPorcentagem { get; set; } = 3.99m;
        public decimal PixEntradaPorcentagem { get; set; } = 0m;
        public decimal PixSaidaPorcentagem { get; set; } = 0m;
        public decimal PixSaidaFixo { get; set; } = 0.23m;
        public decimal BoletoPorcentagem { get; set; } = 0m;
        public decimal BoletoFixo { get; set; } = 0.97m;

    }
}
