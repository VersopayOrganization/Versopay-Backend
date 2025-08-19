using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace VersopayBackend.Dtos
{
    // Use em [FromForm]
    public class DocumentoUploadDto
    {
        // JPG da frente
        public IFormFile? FrenteDoc { get; set; }

        // JPG do verso
        public IFormFile? VersoDoc { get; set; }

        // JPG selfie com documento
        public IFormFile? SelfieDoc { get; set; }

        // PDF do CNPJ (obrigatório se PJ)
        public IFormFile? CartaoCnpjPdf { get; set; }
    }
}
