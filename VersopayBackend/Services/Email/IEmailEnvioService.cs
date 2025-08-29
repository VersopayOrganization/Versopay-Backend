namespace VersopayBackend.Services.Email
{
    public interface IEmailEnvioService
    {
        Task EnvioResetSenhaAsync(string email, string nome, string resetLink, CancellationToken cancellationToken);
        // novo para ativação do device por e-mail
        Task EnvioCodigo2FAAsync(string email, string nome, string code, CancellationToken ct);
    }
}