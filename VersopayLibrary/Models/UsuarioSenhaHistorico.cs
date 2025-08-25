namespace VersopayLibrary.Models
{
    public class UsuarioSenhaHistorico
    {
        public Guid Id { get; set; }
        public int UsuarioId { get; set; }
        public string SenhaHash { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
        public Usuario Usuario { get; set; } = null!;
    }
}