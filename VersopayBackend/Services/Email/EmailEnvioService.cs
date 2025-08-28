using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using VersopayBackend.Options;
using VersopayBackend.Services.Email;

public sealed class EmailEnvioService : IEmailEnvioService
{
    private readonly SmtpSettings _smtp;
    private readonly BrandSettings _brand;

    public EmailEnvioService(IOptions<SmtpSettings> smtp, IOptions<BrandSettings> brand)
    {
        _smtp = smtp.Value;
        _brand = brand.Value;
    }

    public async Task EnvioResetSenhaAsync(string paraEmail, string paraNome, string link, CancellationToken cancellationToken)
    {
        var subject = "Redefinição de senha - VersoPay";
        var html = BuildResetTemplate(paraNome, link, _brand.LogoUrl);

        using var msg = new MailMessage
        {
            From = new MailAddress(_smtp.FromAddress, _smtp.FromName),
            Subject = subject,
            Body = html,
            IsBodyHtml = true
        };
        msg.To.Add(new MailAddress(paraEmail, paraNome));

        using var client = new SmtpClient(_smtp.Host, _smtp.Port)
        {
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            EnableSsl = _smtp.UseSsl,
            Credentials = new NetworkCredential(_smtp.User, _smtp.Pass),
            Timeout = 15000
        };

        await client.SendMailAsync(msg);
    }

    private static string BuildResetTemplate(string nome, string link, string logoUrl)
    {
        return $@"
        <!doctype html>
        <html lang=""pt-BR"">
          <head>
            <meta charset=""utf-8"">
            <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
            <title>Redefinir senha</title>
          </head>
          <body style=""margin:0;background:#f5f6fa;font-family:Inter,Arial,sans-serif;color:#111;"">
            <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"">
              <tr><td align=""center"" style=""padding:30px 16px;"">
                <table role=""presentation"" width=""100%"" style=""max-width:560px;background:#fff;border-radius:12px;box-shadow:0 8px 28px rgba(0,0,0,.08);overflow:hidden;"">
                  <tr>
                    <td style=""padding:28px 24px;text-align:center;border-bottom:1px solid #eee;"">
                      <img src=""{logoUrl}"" alt=""VersoPay"" style=""max-width:180px;height:auto;display:block;margin:0 auto 8px""/>
                    </td>
                  </tr>
                  <tr>
                    <td style=""padding:26px 24px 8px;font-size:16px;line-height:1.5;color:#333;"">
                      <p style=""margin:0 0 12px"">Olá <strong>{System.Net.WebUtility.HtmlEncode(nome)}</strong>,</p>
                      <p style=""margin:0 0 16px"">
                        Um pedido para redefinir sua senha foi realizado. Caso não tenha sido você, basta ignorar este e-mail.
                      </p>
                      <p style=""margin:0 0 22px"">Para criar uma nova senha, clique no botão abaixo:</p>
                      <p style=""text-align:center;margin:0 0 28px"">
                        <a href=""{link}"" style=""display:inline-block;background:linear-gradient(90deg,#c39afc,#6f4ef6);color:#fff;text-decoration:none;
                           padding:14px 22px;border-radius:10px;font-weight:700;"">Redefinir senha</a>
                      </p>
                      <p style=""margin:0;color:#666;font-size:13px"">
                        Se o botão não funcionar, copie e cole este link no seu navegador:<br/>
                        <a href=""{link}"" style=""color:#6f4ef6;word-break:break-all"">{link}</a>
                      </p>
                    </td>
                  </tr>
                  <tr>
                    <td style=""padding:18px 24px 24px;color:#9aa0a6;font-size:12px;text-align:center;border-top:1px solid #f0f0f0"">
                      © {DateTime.Now.Year} VersoPay. Todos os direitos reservados.
                    </td>
                  </tr>
                </table>
              </td></tr>
            </table>
          </body>
        </html>";
    }
}
