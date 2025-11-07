// Dtos/Common/VersellVendorWebhookHandler.cs
using System.Text.Json;
using VersopayBackend.Services;
using VersopayLibrary.Models;
using VersopayLibrary.Enums;
using VersopayBackend.Utils;

namespace VersopayBackend.Dtos.Common
{
    public sealed class VersellVendorWebhookHandler : IVendorWebhookHandler
    {
        private readonly ILogger<VersellVendorWebhookHandler> _logger;

        public VersellVendorWebhookHandler(ILogger<VersellVendorWebhookHandler> logger)
            => _logger = logger;

        public Task<bool> VerifySignatureAsync(int ownerUserId, string rawBody, IDictionary<string, string> headers, CancellationToken ct)
        {
            // Ajuste para a tua lógica real (HMAC/Token).
            // Por enquanto aceita sempre.
            return Task.FromResult(true);
        }

        public string BuildEventKey(string rawBody, IDictionary<string, string> headers)
        {
            // Monte a key a partir do payload real da Versell.
            // Fallback: hash do corpo
            return $"versell:{CryptoUtils.Sha256Base64(rawBody)}".ToLowerInvariant();
        }

        public Task<InboundWebhookLog> ProcessAsync(int ownerUserId, string channel, string rawBody, IDictionary<string, string> headers, string sourceIp, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var log = new InboundWebhookLog
            {
                Provedor = ProvedorWebhook.VersellPay,   // ajuste se tiver enum específico
                Evento = channel?.Contains("pix", StringComparison.OrdinalIgnoreCase) == true
                            ? WebhookEvento.PagamentoPIX
                            : WebhookEvento.Desconhecido,
                EventKey = BuildEventKey(rawBody, headers),
                SourceIp = sourceIp,
                HeadersJson = JsonSerializer.Serialize(headers),
                PayloadJson = rawBody,
                ReceivedAtUtc = now,
                ProcessedAtUtc = now,
                ProcessingStatus = ProcessingStatus.Success,
                DataEventoUtc = now
            };

            // TODO: implementar criação/atualização semelhante ao da Vexy, conforme payload da Versell.
            _logger.LogInformation("Webhook Versell recebido (owner={Owner}, channel={Channel}).", ownerUserId, channel);

            return Task.FromResult(log);
        }
    }
}
