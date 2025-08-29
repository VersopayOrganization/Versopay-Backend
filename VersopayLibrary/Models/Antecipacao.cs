using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VersopayLibrary.Enums;

namespace VersopayLibrary.Models
{
    public class Antecipacao
    {
        public int Id { get; set; }

        // FK -> Usuarios.Id
        public int EmpresaId { get; set; }
        public Usuario Empresa { get; set; } = default!;

        public StatusAntecipacao Status { get; set; } = StatusAntecipacao.PendenteFila;

        // UTC
        public DateTime DataSolicitacao { get; set; } = DateTime.UtcNow;

        // decimal(18,2)
        [Column(TypeName = "decimal(18,2)")]
        public decimal Valor { get; set; }
    }
}
