namespace VersopayBackend.Dtos
{
    public class PasswordResetDtos
    {
        public sealed record SenhaEsquecidaRequest(string Email);
        public sealed record RedefinirSenhaRequest(string Token, string NovaSenha, string Confirmacao);
        public sealed record ValidarResetTokenRequest(string Token);

    }
}