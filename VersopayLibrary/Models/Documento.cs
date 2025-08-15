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
        public int UsuarioId { get; set; }

        // Caminhos relativos (ex.: "uploads/{usuarioId}/frente.jpg")
        [MaxLength(260)]
        public string? FrenteRgCaminho { get; set; }

        [MaxLength(260)]
        public string? VersoRgCaminho { get; set; }

        [MaxLength(260)]
        public string? SelfieDocCaminho { get; set; }

        // Apenas obrigatório quando TipoCadastro = PJ
        [MaxLength(260)]
        public string? CartaoCnpjCaminho { get; set; }

        // --- Status por parte ---
        public StatusDocumento FrenteRgStatus { get; set; } = StatusDocumento.Pendente;
        public StatusDocumento VersoRgStatus { get; set; } = StatusDocumento.Pendente;
        public StatusDocumento SelfieDocStatus { get; set; } = StatusDocumento.Pendente;
        public StatusDocumento CartaoCnpjStatus { get; set; } = StatusDocumento.Pendente;

        // --- Assinatura (hash) por parte ---
        [MaxLength(64)] public string? FrenteRgAssinaturaSha256 { get; set; }
        [MaxLength(64)] public string? VersoRgAssinaturaSha256 { get; set; }
        [MaxLength(64)] public string? SelfieDocAssinaturaSha256 { get; set; }
        [MaxLength(64)] public string? CartaoCnpjAssinaturaSha256 { get; set; }

        public DateTime DataAtualizacao { get; set; } = DateTime.UtcNow;

        // Navegação
        public Usuario Usuario { get; set; } = default!;
    }
}

