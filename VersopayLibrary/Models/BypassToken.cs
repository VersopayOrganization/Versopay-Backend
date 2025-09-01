using System.ComponentModel.DataAnnotations;

namespace VersopayLibrary.Models
{
    public class BypassToken
    {
        public int Id { get; set; }

        [Required] public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = default!;

        [Required, MaxLength(128)] public string TokenHash { get; set; } = default!;
        public DateTime CriadoEmUtc { get; set; } = DateTime.UtcNow;
        public DateTime ExpiraEmUtc { get; set; }
        public DateTime? RevogadoEmUtc { get; set; }
        public DateTime? UltimoUsoUtc { get; set; }

        [MaxLength(64)] public string? Ip { get; set; }
        [MaxLength(200)] public string? UserAgent { get; set; }
        [MaxLength(80)] public string? Dispositivo { get; set; }

        public bool EstaAtivo => RevogadoEmUtc == null && DateTime.UtcNow < ExpiraEmUtc;
    }
}
