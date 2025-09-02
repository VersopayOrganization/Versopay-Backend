using System.ComponentModel.DataAnnotations;

namespace VersopayBackend.Dtos
{
    public sealed class WebhookUpdateDto
    {
        [Required, MaxLength(500), Url]
        public string Url { get; set; } = default!;

        public bool Ativo { get; set; } = true;

        [MaxLength(128)]
        public string? Secret { get; set; }

        [Required, MinLength(1)]
        public string[] Eventos { get; set; } = Array.Empty<string>();
    }
}
