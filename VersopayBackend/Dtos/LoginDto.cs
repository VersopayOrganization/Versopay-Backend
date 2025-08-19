using System.ComponentModel.DataAnnotations;

namespace VersopayBackend.Dtos
{
    public class LoginDto
    {
        [Required, EmailAddress] public string Email { get; set; } = default!;
        [Required] public string Senha { get; set; } = default!;
    }
}
