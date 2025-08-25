using VersopayLibrary.Models;

namespace VersopayBackend.Dtos
{
    public class UsuarioResponseDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = default!;
        public string Email { get; set; } = default!;
        public TipoCadastro? TipoCadastro { get; set; }
        public string? Instagram { get; set; }
        public string? Telefone { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsAdmin { get; set; }
        public string? CpfCnpj { get; set; } = default!;       // só dígitos
        public string? CpfCnpjFormatado { get; set; }         // 000.000.000-00 / 00.000.000/0000-00
    }
}
