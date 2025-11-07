using System.Linq; // Where(char.IsDigit)
using System.Text.Json;
using VersopayBackend.Dtos;
using VersopayBackend.Dtos.VexyBank;
using VersopayBackend.Repositories;
using VersopayBackend.Repositories.VexyClient.PixIn;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Services.Vexy
{
    public sealed class VexyBankService(
        IVexyBankClient client,
        IInboundWebhookLogRepository inboundLogRepo,
        IProviderCredentialRepository credRepo,
        IVexyBankPixInRepository pixInRepo,
        ILogger<VexyBankService> logger
    ) : IVexyBankService
    {
        private readonly IVexyBankClient _client = client;
        private readonly IInboundWebhookLogRepository _inboundLogRepo = inboundLogRepo;
        private readonly IProviderCredentialRepository _credRepo = credRepo;
        private readonly IVexyBankPixInRepository _pixInRepo = pixInRepo;
        private readonly ILogger<VexyBankService> _logger = logger;

        private static string BuildUserWebhookUrl(string publicBaseUrl, int ownerUserId, string channel) =>
            $"{publicBaseUrl.TrimEnd('/')}/api/webhooks/v1/vexy/{ownerUserId}/{channel}";

        public async Task<(bool ok, string? error)> ValidateCredentialsAsync(int ownerUserId, CancellationToken ct)
        {
            try
            {
                // 1) precisa existir o registro
                var cred = await _credRepo.GetAsync(ownerUserId, PaymentProvider.Vexy, ct);
                if (cred is null)
                    return (false, "Credenciais não encontradas para este usuário.");

                // 2) precisa ter ApiKey e ApiSecret (public/secret)
                if (string.IsNullOrWhiteSpace(cred.ApiKey) || string.IsNullOrWhiteSpace(cred.ApiSecret))
                    return (false, "ApiKey (public) e ApiSecret (secret) não foram informadas.");

                // 3) Tenta gerar um JWT via Basic (apiKey:apiSecret).
                //    Se conseguir, as credenciais estão ok.
                var token = await _client.EnsureJwtAsync(ownerUserId, ct);
                return string.IsNullOrWhiteSpace(token)
                    ? (false, "Não foi possível obter token JWT na VexyBank.")
                    : (true, null);
            }
            catch (OperationCanceledException)
            {
                return (false, "Operação cancelada.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar credenciais VexyBank (OwnerUserId={Owner})", ownerUserId);
                return (false, ex.Message);
            }
        }

        public async Task<PixInCreateRespDto> CreatePixInAsync(int ownerUserId, PixInCreateReqDto req, CancellationToken ct, int? pedidoId = null)
        {
            _ = await _credRepo.GetAsync(ownerUserId, PaymentProvider.Vexy, ct)
                ?? throw new InvalidOperationException("Credenciais Vexy não configuradas para este usuário.");

            if (string.IsNullOrWhiteSpace(req.PostbackUrl))
            {
                var publicBase = Environment.GetEnvironmentVariable("PUBLIC_BASE_URL") ?? "https://versopay.com.br";
                req.PostbackUrl = $"{publicBase.TrimEnd('/')}/api/webhooks/v1/vexy/{ownerUserId}/pix-in";
            }

            // Normalizações
            req.Customer.Document = new string((req.Customer.Document ?? "").Where(char.IsDigit).ToArray());
            req.Customer.DocumentType = req.Customer.Document?.Length == 14 ? "cnpj" : "cpf";

            var resp = await _client.PostAsync<PixInCreateReqDto, PixInCreateRespDto>(
                ownerUserId, "/api/v1/pix/in/qrcode", req, ct);

            // >>> Salva localmente o PIX IN criado
            var entity = new VexyBankPixIn
            {
                OwnerUserId = ownerUserId,
                ExternalId = resp.Data.Id,             // "cmhnd0tb900qt1mib0ihtatrw"
                Status = resp.Data.Status,         // "pending"
                AmountCents = null,                     // preencha se você tiver o valor
                PixEmv = resp.Data.Pix?.Emv,
                QrPngBase64 = resp.Data.Pix?.QrCodeBase64,
                PostbackUrl = req.PostbackUrl,
                PedidoId = pedidoId ?? 0
            };



            await _pixInRepo.AddAsync(entity, ct);
            await _pixInRepo.SaveChangesAsync(ct);

            return resp;
        }

        public async Task<PixOutRespDto> SendPixOutAsync(int ownerUserId, PixOutReqDto req, string idempotencyKey, CancellationToken ct)
        {
            _ = await _credRepo.GetAsync(ownerUserId, PaymentProvider.Vexy, ct)
                ?? throw new InvalidOperationException("Credenciais Vexy não configuradas para este usuário.");

            if (string.IsNullOrWhiteSpace(req.PostbackUrl))
            {
                var publicBase = Environment.GetEnvironmentVariable("PUBLIC_BASE_URL") ?? "https://versopay.com.br";
                req.PostbackUrl = BuildUserWebhookUrl(publicBase, ownerUserId, "pix-out");
            }

            if (string.IsNullOrWhiteSpace(idempotencyKey))
                throw new InvalidOperationException("X-Idempotency-Key é obrigatório para Pix Out.");

            // ✅ agora manda o header x-idempotency-key
            var resp = await _client.PostAsync<PixOutReqDto, PixOutRespDto>(
                ownerUserId,
                "/api/v1/pix/out/pixkey",
                req,
                idempotencyKey,
                ct);

            return resp;
        }


        public async Task HandleWebhookAsync(
        int ownerUserId,
        VexyWebhookEnvelope payload,
        string? sourceIp,
        IDictionary<string, string>? headers,
        CancellationToken ct)
        {
            // --- dedupe + log já existentes no seu código ---
            var eventKey = $"vexy:{ownerUserId}:{payload.Type}:{payload.Event}:{payload.Id}".ToLowerInvariant();
            if (await _inboundLogRepo.ExistsByEventKeyAsync(eventKey, ct)) return;

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

            await _inboundLogRepo.AddAsync(log, ct);
            await _inboundLogRepo.SaveChangesAsync(ct);

            // --- ATUALIZA o VexyBankPixIn quando for transaction ---
            if (payload.Type?.Equals("transaction", StringComparison.OrdinalIgnoreCase) == true &&
                payload.Transaction is not null &&
                !string.IsNullOrWhiteSpace(payload.Transaction.Id))
            {
                var extId = payload.Transaction.Id!;
                var local = await _pixInRepo.FindByExternalIdAsync(ownerUserId, extId, ct);
                if (local is not null)
                {
                    local.Status = payload.Transaction.Status?.ToLowerInvariant();

                    // tenta obter o documento do pagador
                    var doc =
                        payload.Transaction.Pix?.PayerInfo?.Document ??
                        payload.Transaction.PayerDocument;

                    if (!string.IsNullOrWhiteSpace(doc))
                        local.PayerDocument = doc;

                    if (string.Equals(local.Status, "paid", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(local.Status, "completed", StringComparison.OrdinalIgnoreCase))
                    {
                        local.PaidAtUtc = DateTime.UtcNow;
                    }

                    local.UpdatedAtUtc = DateTime.UtcNow;
                    await _pixInRepo.SaveChangesAsync(ct);
                }
            }

            // aqui você pode disparar efeitos de domínio (aprovar pedido, lançar extrato etc.)
        }


        public async Task<PixInStatusRespDto> GetPixInAsync(int ownerUserId, string id, CancellationToken ct)
        {
            // Garante credenciais e obtém/renova JWT internamente via client
            _ = await _credRepo.GetAsync(ownerUserId, PaymentProvider.Vexy, ct)
                ?? throw new InvalidOperationException("Credenciais Vexy não configuradas para este usuário.");

            // A doc manda consultar /api/v1/transactions/{id}
            var path = $"/api/v1/transactions/{id}";

            try
            {
                var resp = await _client.GetAsync<PixInStatusRespDto>(ownerUserId, path, ct);
                return resp ?? throw new InvalidOperationException("Resposta vazia ao consultar transação PIX IN.");
            }
            catch (InvalidOperationException ex)
            {
                // Se seu client padroniza a mensagem de 404, preserve a dica:
                throw new InvalidOperationException(
                    $"Nenhuma rota de consulta PixIn funcionou para id '{id}'. " +
                    "Use /api/v1/transactions/{id} conforme documentação.",
                    ex);
            }
        }
    }
}
