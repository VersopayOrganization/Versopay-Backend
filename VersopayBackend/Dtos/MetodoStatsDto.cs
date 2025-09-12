namespace VersopayBackend.Dtos
{
    public sealed class MetodoStatsDto
    {
        public string Metodo { get; set; } = "";              // "Cartao" | "Pix" | "Boleto"
        public decimal AprovacaoPercent { get; set; }
        public int QtdePedidos { get; set; }                  // total de tentativas
        public decimal TotalPedidos { get; set; }             // soma (todas as tentativas)
        public int QtdeAprovados { get; set; }
        public decimal TotalAprovados { get; set; }
    }
}
