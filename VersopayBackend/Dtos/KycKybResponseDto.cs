using VersopayLibrary.Enums;

namespace VersopayBackend.Dtos
{
    public class KycKybResponseDto
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public StatusKycKyb Status { get; set; }
        public string CpfCnpj { get; set; } = default!;
        public string? CpfCnpjFormatado { get; set; }
        public string Nome { get; set; } = default!;
        public string? NumeroDocumento { get; set; }
        public DateTime? DataAprovacao { get; set; }
    }
}
