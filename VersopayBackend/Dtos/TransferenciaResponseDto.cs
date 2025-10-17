using VersopayLibrary.Enums;

namespace VersopayBackend.Dtos
{
    public sealed class TransferenciaResponseDto
    {
        public int Id { get; set; }
        public int SolicitanteId { get; set; }
        public StatusTransferencia Status { get; set; }
        public DateTime DataSolicitacao { get; set; }
        public decimal ValorSolicitado { get; set; }

        public string? Nome { get; set; }
        public string? Empresa { get; set; }
        public string? ChavePix { get; set; }
        public AprovacaoManual Aprovacao { get; set; }
        public TipoEnvioManual? TipoEnvio { get; set; }
        public decimal? Taxa { get; set; }
        public decimal? ValorFinal { get; set; }

        public DateTime DataCadastro { get; set; }
        public DateTime? DataAprovacao { get; set; }
        public MetodoPagamento  MetodoPagamento { get; set; }  
    }
}
