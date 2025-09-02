using System.ComponentModel.DataAnnotations;

namespace VersopayBackend.Dtos
{
    public sealed class WebhookCreateDto
    {
        [Required, MaxLength(500), Url]
        public string Url { get; set; } = default!;

        public bool Ativo { get; set; } = true;

        // Opcional (para HMAC). Se não quiser expor no response, beleza.
        [MaxLength(128)]
        public string? Secret { get; set; }

        // nomes do enum (ex.: ["PixGerado","CompraAprovada"])
        [Required, MinLength(1)]
        public string[] Eventos { get; set; } = Array.Empty<string>();
    }
}
