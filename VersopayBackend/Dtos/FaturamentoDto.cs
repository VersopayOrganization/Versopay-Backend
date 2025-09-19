namespace VersopayBackend.Dtos
{
    public sealed class FaturamentoDto
    {
        public int Id { get; set; }
        public string CpfCnpj { get; set; } = default!;
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }

        public decimal VendasTotais { get; set; }
        public decimal VendasCartao { get; set; }
        public decimal VendasBoleto { get; set; }
        public decimal VendasPix { get; set; }
        public decimal Reserva { get; set; }

        public int VendasCanceladas { get; set; }
        public int DiasSemVendas { get; set; }
        public DateTime AtualizadoEmUtc { get; set; }
    }
}
