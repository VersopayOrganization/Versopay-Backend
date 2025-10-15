using VersopayLibrary.Enums;

namespace VersopayBackend.Dtos
{
    public class KycKybResponseDto
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public StatusKycKyb Status { get; set; }
        public string Cpf { get; set; } = default!;
        public string Cnpj { get; set; } = default!;
        public string? CpfFormatado { get; set; }
        public string? CnpjFormatado { get; set; }
        public string Nome { get; set; } = default!;
        public string? NumeroDocumento { get; set; }
        public DateTime? DataAprovacao { get; set; }
    }
}
