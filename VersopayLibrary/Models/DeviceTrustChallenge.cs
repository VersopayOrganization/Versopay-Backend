using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersopayLibrary.Models
{
    public class DeviceTrustChallenge
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        [Required] public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = default!;
        [MaxLength(128)] public string CodeHash { get; set; } = default!;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAtUtc { get; set; }
        public bool Used { get; set; } = false;

        [MaxLength(64)] public string? Ip { get; set; }
        [MaxLength(200)] public string? UserAgent { get; set; }
        [MaxLength(80)] public string? Dispositivo { get; set; }
    }
}
