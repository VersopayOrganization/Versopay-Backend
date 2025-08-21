namespace VersopayBackend.Services.Email
{
    public class EmailEnvioService(ILogger<EmailEnvioService> logger) : IEmailEnvioService
    {
        public Task EnvioResetSenhaAsync(string email, string nome, string resetLink, CancellationToken cancellationToken)
        {
            logger.LogInformation("E-mail de reset -> {Email} | Link: {Link}", email, resetLink);
            return Task.CompletedTask;
        }
    }
}