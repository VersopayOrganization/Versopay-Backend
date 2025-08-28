using System.ComponentModel.DataAnnotations;
using VersopayLibrary.Enums;

namespace VersopayBackend.Dtos
{
    public class AntecipacaoStatusUpdateDto
    {
        [Required]
        public StatusAntecipacao Status { get; set; }
    }
}
