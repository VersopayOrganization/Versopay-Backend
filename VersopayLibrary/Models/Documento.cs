using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;

namespace VersopayLibrary.Models
{
    // Tabela para arquivos de verificação de identidade
    public class Documento
    {
        // PK = FK para Usuario (relacionamento 1:1)
        [Key]
        public Guid UsuarioId { get; set; }

        // Caminhos relativos (ex.: "uploads/{usuarioId}/frente.jpg")
        [MaxLength(260)]
        public string? FrenteRgCnhPath { get; set; }

        [MaxLength(260)]
        public string? VersoRgCnhPath { get; set; }

        [MaxLength(260)]
        public string? SelfieComDocPath { get; set; }

        // Apenas obrigatório quando TipoCadastro = PJ
        [MaxLength(260)]
        public string? CartaoCnpjPdfPath { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navegação
        public Usuario Usuario { get; set; } = default!;
    }
}

