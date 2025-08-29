using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using VersopayBackend.Options;
using VersopayBackend.Services.Email;

public sealed class EmailEnvioService : IEmailEnvioService
{
    private readonly SmtpSettings _smtp;
    private readonly BrandSettings _brand;
    private readonly IWebHostEnvironment _env;

    public EmailEnvioService(
        IOptions<SmtpSettings> smtp,
        IOptions<BrandSettings> brand,
        IWebHostEnvironment env)
    {
        _smtp = smtp.Value;
        _brand = brand.Value;
        _env = env;
    }

    public async Task EnvioResetSenhaAsync(string paraEmail, string paraNome, string link, CancellationToken ct)
    {
        const string cidLogo = "logo-versopay";
        var logoPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "email", "logo-versopay.png");
        var hasLocalLogo = File.Exists(logoPath);

        var html = BuildResetTemplate(
            nome: paraNome,
            link: link,
            cidLogo: hasLocalLogo ? cidLogo : null,
            logoUrlFallback: !hasLocalLogo ? _brand.LogoUrl : null
        );

        using var msg = new MailMessage
        {
            From = new MailAddress(_smtp.FromAddress, _smtp.FromName),
            Subject = "Redefinição de senha - VersoPay",
            IsBodyHtml = true,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8,
            HeadersEncoding = Encoding.UTF8
        };
        msg.To.Add(new MailAddress(paraEmail, paraNome));

        // View HTML
        var htmlView = AlternateView.CreateAlternateViewFromString(html, Encoding.UTF8, MediaTypeNames.Text.Html);

        // Anexa a logo como CID (se existir localmente)
        if (hasLocalLogo)
        {
            // MIME certo para PNG
            var logo = new LinkedResource(logoPath, "image/png")
            {
                ContentId = cidLogo,
                TransferEncoding = TransferEncoding.Base64
            };
            // Alguns clientes exigem explicitamente inline:
            logo.ContentType.Name = "logo-versopay.png";
            logo.ContentLink = new Uri($"cid:{cidLogo}");
            htmlView.LinkedResources.Add(logo);
        }

        msg.AlternateViews.Add(htmlView);

        using var client = new SmtpClient(_smtp.Host, _smtp.Port)
        {
            EnableSsl = _smtp.UseSsl,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_smtp.User, _smtp.Pass),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        await client.SendMailAsync(msg);
    }

    private static string BuildResetTemplate(string nome, string link, string? cidLogo, string? logoUrlFallback)
    {
        // Construção da tag de imagem (CID > URL > sem imagem)
        string logoTag = "";
        if (!string.IsNullOrWhiteSpace(cidLogo))
            logoTag = $"<img src=\"cid:{cidLogo}\" alt=\"VersoPay\" width=\"140\" height=\"auto\" style=\"display:block\"/>";
        else if (!string.IsNullOrWhiteSpace(logoUrlFallback))
            logoTag = $"<img src=\"{logoUrlFallback}\" alt=\"VersoPay\" width=\"140\" height=\"auto\" style=\"display:block\"/>";

        // Botão à prova de Outlook (VML) + fallback HTML
        string button = $@"
        <!--[if mso]>
        <v:roundrect xmlns:v=""urn:schemas-microsoft-com:vml""
                     href=""{WebUtility.HtmlEncode(link)}""
                     style=""height:44px;v-text-anchor:middle;width:240px;""
                     arcsize=""12%"" strokecolor=""#6f4ef6"" fillcolor=""#6f4ef6"">
          <w:anchorlock/>
          <center style=""color:#ffffff;font-family:Arial,sans-serif;font-size:16px;font-weight:700;"">
            Redefinir senha
          </center>
        </v:roundrect>
        <![endif]-->
        <!--[if !mso]><!-- -->
        <a href=""{WebUtility.HtmlEncode(link)}""
           style=""background:#6f4ef6;border-radius:10px;color:#ffffff;display:inline-block;
                  font-weight:700;line-height:44px;text-align:center;text-decoration:none;
                  width:240px;mso-hide:all;"">
          Redefinir senha
        </a>
        <!--<![endif]-->";

        var nomeSafe = WebUtility.HtmlEncode(nome);
        var linkSafe = WebUtility.HtmlEncode(link);

        return $@"
            <!doctype html>
            <html lang=""pt-BR"">
              <head>
                <meta charset=""utf-8"">
                <meta name=""viewport"" content=""width=device-width,initial-scale=1"">
                <title>Redefinir senha</title>
              </head>
              <body style=""margin:0;background:#f5f6fa;font-family:Arial, Helvetica, sans-serif;color:#111;"">
                <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"">
                  <tr><td align=""center"" style=""padding:30px 16px;"">
                    <table role=""presentation"" width=""100%"" style=""max-width:560px;background:#fff;border-radius:12px;box-shadow:0 8px 28px rgba(0,0,0,.08);overflow:hidden;"">
                      <tr>
                        <td style=""padding:28px 24px;text-align:center;border-bottom:1px solid #eee; margin-left: auto; margin-right: auto;"">
                          {logoTag}
                          <div style=""font-size:0;line-height:0;height:0"">&nbsp;</div>
                        </td>
                      </tr>
                      <tr>
                        <td style=""padding:24px;font-size:16px;line-height:1.5;color:#333;"">
                          <p style=""margin:0 0 12px"">Olá <strong>{nomeSafe}</strong>,</p>
                          <p style=""margin:0 0 16px"">
                            Um pedido para redefinir sua senha foi realizado. Caso não tenha sido você, basta ignorar este e-mail.
                          </p>
                          <p style=""margin:0 0 22px"">Para criar uma nova senha, clique no botão abaixo:</p>
                          <p style=""text-align:center;margin:0 0 28px"">
                            {button}
                          </p>
                          <p style=""margin:0;color:#666;font-size:13px"">
                            Se o botão não funcionar, copie e cole este link no seu navegador:<br/>
                            <a href=""{linkSafe}"" style=""color:#6f4ef6;word-break:break-all"">{linkSafe}</a>
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
