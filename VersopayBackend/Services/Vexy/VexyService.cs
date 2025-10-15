using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using VersopayBackend.Dtos;
using VersopayBackend.Repositories;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Services
{
    public sealed class VexyService(
        IVexyClient client,
        IInboundWebhookLogRepository inboundLogRepo,
        IProviderCredentialRepository credRepo,
        ILogger<VexyService> logger
    ) : IVexyService
    {
        static readonly JsonSerializerOptions _jsonOpts = new()
        {
            // iremos honrar os [JsonPropertyName], então pode deixar null
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        public async Task<(bool ok, string? error)> ValidateCredentialsAsync(int ownerUserId, CancellationToken ct)
        {
            try
            {
                // Se conseguir token, credenciais estão válidas
                _ = await client.GetAccessTokenAsync(ownerUserId, ct);
                return (true, null);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Falha ao validar credenciais Vexy (OwnerUserId={Owner})", ownerUserId);
                return (false, ex.Message);
            }
        }

        public async Task<VexyDepositRespDto> CreateDepositAsync(int ownerUserId, VexyDepositReqDto req, CancellationToken ct)
        {
            // normalizações
            req.payer.document = new string((req.payer.document ?? "").Where(char.IsDigit).ToArray());
            if (string.IsNullOrWhiteSpace(req.payer.document_type))
            {
                req.payer.document_type = req.payer.document?.Length switch
                {
                    11 => "CPF",
                    14 => "CNPJ",
                    _ => null
                };
            }

            if (string.IsNullOrWhiteSpace(req.clientCallbackUrl) ||
                req.clientCallbackUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("clientCallbackUrl inválido: use um endpoint HTTPS público.");

            // chama a Vexy
            var data = await client.PostAsync<VexyDepositReqDto, VexyDepositRespDto>(
                ownerUserId, "/api/payments/deposit", req, ct);

            return data;
        }

        public async Task<VexyWithdrawRespDto> RequestWithdrawalAsync(int ownerUserId, VexyWithdrawReqDto req, CancellationToken ct)
        {
            var resp = await client.PostAsync(ownerUserId, "/api/withdrawals/withdraw", req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"Vexy withdraw failed: {(int)resp.StatusCode} {resp.ReasonPhrase} - {body}");

            var data = System.Text.Json.JsonSerializer.Deserialize<VexyWithdrawRespDto>(
                body, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return data ?? throw new InvalidOperationException("Resposta vazia/inesperada da Vexy (withdraw).");
        }

        public async Task LogMedAsync(int ownerUserId, VexyMedLogDto med, string? sourceIp, IDictionary<string, string>? headers, CancellationToken ct)
        {
            // Garante que o dono tem credenciais Vexy
            _ = await credRepo.GetAsync(ownerUserId, PaymentProvider.Vexy, ct)
                ?? throw new InvalidOperationException("Credenciais Vexy não configuradas para este usuário.");

            var eventKey = $"vexy-med:{med.transaction_id}:{med.status}".ToLowerInvariant();
            if (await inboundLogRepo.ExistsByEventKeyAsync(eventKey, ct)) return;

            var log = new InboundWebhookLog
            {
                Provedor = ProvedorWebhook.VexyPayments,
                Evento = WebhookEvento.SaqueRetidoMED,
                EventKey = eventKey,
                SourceIp = sourceIp ?? string.Empty,
                HeadersJson = JsonSerializer.Serialize(headers ?? new Dictionary<string, string>()),
                PayloadJson = JsonSerializer.Serialize(med),
                ReceivedAtUtc = DateTime.UtcNow,
                ProcessedAtUtc = med.processed_at ?? DateTime.UtcNow,
                ProcessingStatus = ProcessingStatus.Success,
                TransactionId = med.transaction_id,
                Status = med.status,
                DataEventoUtc = DateTime.SpecifyKind(med.timestamp, DateTimeKind.Utc)
            };

            await inboundLogRepo.AddAsync(log, ct);
            await inboundLogRepo.SaveChangesAsync(ct);
        }
    }
}
