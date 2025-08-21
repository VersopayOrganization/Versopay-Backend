using System.ComponentModel.DataAnnotations;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Dtos
{
    public class KycKybStatusUpdateDto
    {
        [Required] public StatusKycKyb Status { get; set; }
    }
}
