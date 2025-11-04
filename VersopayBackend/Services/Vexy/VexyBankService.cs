using System.Text.Json;
using VersopayBackend.Dtos;
using VersopayBackend.Dtos.VexyBank;
using VersopayBackend.Repositories;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Services.Vexy
{
    public sealed class VexyBankService(
    IVexyBankClient client,
    IInboundWebhookLogRepository inboundLogRepo,
    IProviderCredentialRepository credRepo,
    ILogger<VexyBankService> logger) : IVexyBankService
    {
        // Gera a URL de postback por usuário (pode vir do appsettings Brand:PublicBaseUrl)
        private static string BuildUserWebhookUrl(string publicBaseUrl, int ownerUserId, string channel) =>
            $"{publicBaseUrl.TrimEnd('/')}/api/webhooks/v1/vexy/{ownerUserId}/{channel}";

        public async Task<PixInCreateRespDto> CreatePixInAsync(int ownerUserId, PixInCreateReqDto req, CancellationToken ct)
        {
            // garante credenciais
            _ = await credRepo.GetAsync(ownerUserId, PaymentProvider.Vexy, ct)
                ?? throw new InvalidOperationException("Credenciais Vexy não configuradas para este usuário.");

            // postbackUrl: se não veio, configuramos a padrão do usuário
            if (string.IsNullOrWhiteSpace(req.PostbackUrl))
            {
                // pegue de BrandSettings ou IOptions<BrandSettings>
                var publicBase = Environment.GetEnvironmentVariable("PUBLIC_BASE_URL")
                                 ?? "https://versopay.com.br";
                req.PostbackUrl = BuildUserWebhookUrl(publicBase, ownerUserId, "pix-in");
            }

            // normalizações simples
            req.Customer.Document = new string((req.Customer.Document ?? "").Where(char.IsDigit).ToArray());
            req.Customer.DocumentType = req.Customer.Document?.Length == 14 ? "cnpj" : "cpf";

            var resp = await client.PostAsync<PixInCreateReqDto, PixInCreateRespDto>(
                ownerUserId, "/api/v1/pix/in/qrcode", req, ct);

            return resp;
        }

        public async Task<PixOutRespDto> SendPixOutAsync(int ownerUserId, PixOutReqDto req,
            string idempotencyKey, CancellationToken ct)
        {
            _ = await credRepo.GetAsync(ownerUserId, PaymentProvider.Vexy, ct)
                ?? throw new InvalidOperationException("Credenciais Vexy não configuradas para este usuário.");

            if (string.IsNullOrWhiteSpace(req.PostbackUrl))
            {
                var publicBase = Environment.GetEnvironmentVariable("PUBLIC_BASE_URL")
                                 ?? "https://versopay.com.br";
                req.PostbackUrl = BuildUserWebhookUrl(publicBase, ownerUserId, "pix-out");
            }

            // header de idempotência é CRÍTICO no Pix Out
            var headers = new Dictionary<string, string> { ["x-idempotency-key"] = idempotencyKey };

            return await client.PostAsync<PixOutReqDto, PixOutRespDto>(
                ownerUserId, "/api/v1/pix/out/pixkey", req, ct, headers);
        }

        public async Task HandleWebhookAsync(int ownerUserId, VexyWebhookEnvelope payload,
            string? sourceIp, IDictionary<string, string>? headers, CancellationToken ct)
        {
            // dedupe por "id" do webhook + tipo + owner
            var eventKey = $"vexy:{ownerUserId}:{payload.Type}:{payload.Event}:{payload.Id}".ToLowerInvariant();
            if (await inboundLogRepo.ExistsByEventKeyAsync(eventKey, ct)) return;

            var log = new InboundWebhookLog
            {
                Provedor = ProvedorWebhook.VexyPayments,
                Evento = payload.Type?.Equals("transaction", StringComparison.OrdinalIgnoreCase) == true
                            ? WebhookEvento.PagamentoPIX
                            : WebhookEvento.TransferenciaPIX,
                EventKey = eventKey,
                SourceIp = sourceIp ?? "",
                HeadersJson = JsonSerializer.Serialize(headers ?? new Dictionary<string, string>()),
                PayloadJson = JsonSerializer.Serialize(payload),
                ReceivedAtUtc = DateTime.UtcNow,
                ProcessedAtUtc = DateTime.UtcNow,
                ProcessingStatus = ProcessingStatus.Success,
                TransactionId = payload.Transaction?.Id ?? payload.Transfer?.Id,
                Status = payload.Transaction?.Status ?? payload.Transfer?.Status,
                DataEventoUtc = DateTime.UtcNow
            };

            await inboundLogRepo.AddAsync(log, ct);
            await inboundLogRepo.SaveChangesAsync(ct);

            // TODO: aqui você pode acionar serviços de domínio (liberar pedido, atualizar extrato, etc.)
        }
    }
}
