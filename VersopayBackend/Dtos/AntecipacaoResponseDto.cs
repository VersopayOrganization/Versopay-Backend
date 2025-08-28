using VersopayLibrary.Enums;

namespace VersopayBackend.Dtos
{
    public class AntecipacaoResponseDto
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public string? EmpresaNome { get; set; }
        public StatusAntecipacao Status { get; set; }
        public DateTime DataSolicitacao { get; set; }
        public decimal Valor { get; set; }
    }
}
