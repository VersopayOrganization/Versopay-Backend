namespace VersopayBackend.Dtos
{
    public sealed class WebhookResponseDto
    {
        public int Id { get; set; }
        public string Url { get; set; } = default!;
        public bool Ativo { get; set; }
        public bool HasSecret { get; set; }
        public string[] Eventos { get; set; } = Array.Empty<string>();
        public int EventosMask { get; set; }
        public DateTime CriadoEmUtc { get; set; }
        public DateTime? AtualizadoEmUtc { get; set; }
    }
}
