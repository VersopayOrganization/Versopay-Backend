using System.ComponentModel.DataAnnotations;
using VersopayLibrary.Enums;

namespace VersopayBackend.Dtos
{
    public class KycKybCreateDto
    {
        [Required] public int UsuarioId { get; set; }

        // envie 0/1 ou "Aprovado"/"Reprovado"
        [Required] public StatusKycKyb Status { get; set; }

        // opcional: RG/CNH
        [MaxLength(64)] public string? NumeroDocumento { get; set; }
    }
}
