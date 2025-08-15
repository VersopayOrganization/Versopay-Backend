namespace VersopayBackend.Dtos
{
    public class DocumentoResponseDto
    {
        public Guid UsuarioId { get; set; }
        public string? FrenteUrl { get; set; }
        public string? VersoUrl { get; set; }
        public string? SelfieUrl { get; set; }
        public string? CartaoCnpjUrl { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
