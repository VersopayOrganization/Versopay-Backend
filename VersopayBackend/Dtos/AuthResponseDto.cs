namespace VersopayBackend.Dtos
{
    public class AuthResponseDto
    {
        public string AccessToken { get; set; } = default!;
        public DateTime ExpiresAtUtc { get; set; }
        public UsuarioResponseDto Usuario { get; set; } = default!;
    }
}
