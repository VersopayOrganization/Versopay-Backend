using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersopayLibrary.Enums;

namespace VersopayLibrary.Models
{
    public class Webhook
    {
        public int Id { get; set; }

        [Required, MaxLength(500)]
        public string Url { get; set; } = default!;

        public bool Ativo { get; set; } = true;

        // usado p/ assinar payloads (HMAC). Armazene criptografado se quiser.
        [MaxLength(128)]
        public string? Secret { get; set; }

        // bitmask (Flags)
        public WebhookEvent Eventos { get; set; } = WebhookEvent.None;

        public DateTime CriadoEmUtc { get; set; } = DateTime.UtcNow;
        public DateTime? AtualizadoEmUtc { get; set; }
    }
}
