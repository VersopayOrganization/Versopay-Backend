using System.ComponentModel.DataAnnotations;

namespace VersopayLibrary.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }

        [Required] public int UsuarioId { get; set; }
        [Required, MaxLength(128)] public string TokenHash { get; set; } = default!; // SHA-256 em hex
        public DateTime CriadoEmUtc { get; set; } = DateTime.UtcNow;
        public DateTime ExpiraEmUtc { get; set; }
        public DateTime? RevogadoEmUtc { get; set; }
        [MaxLength(128)] public string? SubstituidoPorHash { get; set; }

        // Metadados (opcional)
        [MaxLength(64)] public string? Ip { get; set; }
        [MaxLength(200)] public string? UserAgent { get; set; }
        [MaxLength(80)] public string? Dispositivo { get; set; }

        public Usuario Usuario { get; set; } = default!;
        public bool EstaAtivo => RevogadoEmUtc == null && DateTime.UtcNow < ExpiraEmUtc;
    }
}
