namespace VersopayLibrary.Models
{
    public class NovaSenhaResetToken
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string TokenHash { get; set; } = null!;
        public DateTime DataSolicitacao { get; set; }
        public DateTime DataExpiracao { get; set; }
        public DateTime? DataTokenUsado { get; set; }
        public string? Ip { get; set; }
        public string? UserAgent { get; set; }
        public bool EstaAtivo(DateTime agoraUtc) => DataTokenUsado is null && DataExpiracao > agoraUtc;
        public Usuario Usuario { get; set; } = default!;
    }
}