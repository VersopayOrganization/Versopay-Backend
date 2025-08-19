using VersopayLibrary.Models;

namespace VersopayBackend.Dtos
{
    public class DocumentoResponseDto
    {
        public int UsuarioId { get; set; }

        // URLs SAS temporárias (se solicitadas)
        public string? FrenteUrl { get; set; }
        public string? VersoUrl { get; set; }
        public string? SelfieUrl { get; set; }
        public string? CartaoCnpjUrl { get; set; }
        public DateTime? UrlsExpiramEm { get; set; }

        // Status por parte
        public StatusDocumento FrenteRgStatus { get; set; }
        public StatusDocumento VersoRgStatus { get; set; }
        public StatusDocumento SelfieDocStatus { get; set; }
        public StatusDocumento CartaoCnpjStatus { get; set; }

        // Assinatura (hash) por parte
        public string? FrenteRgAssinaturaSha256 { get; set; }
        public string? VersoRgAssinaturaSha256 { get; set; }
        public string? SelfieDocAssinaturaSha256 { get; set; }
        public string? CartaoCnpjAssinaturaSha256 { get; set; }

        public DateTime UploadedAt { get; set; }
    }
}
