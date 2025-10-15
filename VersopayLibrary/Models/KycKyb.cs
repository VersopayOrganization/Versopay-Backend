using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersopayLibrary.Enums;

namespace VersopayLibrary.Models
{
    // Registro de análise KYC/KYB (histórico)
    public class KycKyb
    {
        public int Id { get; set; }
        // FK -> Usuarios.Id
        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = default!;
        public StatusKycKyb Status { get; set; }
        // Snapshots para histórico
        [MaxLength(11)] 
        public string? Cpf { get; set; }   // só dígitos
        [MaxLength(14)] 
        public string? Cnpj { get; set; }  // só dígitos
        public string Nome { get; set; } = default!;          // nvarchar(120)
        public string? NumeroDocumento { get; set; }          // ex.: RG/CNH (nvarchar(64))
        // Preenchida somente quando Status = Aprovado
        public DateTime? DataAprovacao { get; set; }
    }
}
