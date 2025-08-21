namespace VersopayBackend.Services.Email
{
    public interface IEmailEnvioService
    {
        Task EnvioResetSenhaAsync(string email, string nome, string resetLink, CancellationToken cancellationToken);
    }
}