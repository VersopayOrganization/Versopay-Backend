
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Net.Http.Json;
using VersopayBackend.Dtos;
using VersopayBackend.Repositories;
using VersopayBackend.Repositories.Webhook;
using VersopayBackend.Utils;
using VersopayLibrary.Enums;
using WebhookModel = VersopayLibrary.Models.Webhook; 

namespace VersopayBackend.Services.Webhook
{
    public sealed class WebhooksService(IWebhookRepository webhookRepository, ILogger<WebhooksService> logger) : IWebhooksService
    {
        public async Task<WebhookResponseDto> CreateAsync(WebhookCreateDto webhookCreateDto, CancellationToken cancellationToken)
        {
            var eventos = ParseEventos(webhookCreateDto.Eventos);

            var entity = new WebhookModel
            {
                Url = webhookCreateDto.Url.Trim(),
                Ativo = webhookCreateDto.Ativo,
                Secret = string.IsNullOrWhiteSpace(webhookCreateDto.Secret) ? null : webhookCreateDto.Secret,
                Eventos = eventos,
                CriadoEmUtc = DateTime.UtcNow
            };

            await webhookRepository.AddAsync(entity, cancellationToken);
            await webhookRepository.SaveChangesAsync(cancellationToken);

            return Map(entity);
        }

        public async Task<IEnumerable<WebhookResponseDto>> GetAllAsync(bool? ativo, CancellationToken cancellationToken)
        {
            var query = webhookRepository.QueryNoTracking().OrderByDescending(webhookModel => webhookModel.CriadoEmUtc);
            if (ativo.HasValue)
                query = query.Where(webhookModel => webhookModel.Ativo == ativo.Value).OrderByDescending(x => x.CriadoEmUtc);

            var lista = await query.ToListAsync(cancellationToken);
            return lista.Select(Map);
        }

        public async Task<WebhookResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            var webhook = await webhookRepository.GetByIdNoTrackingAsync(id, cancellationToken);
            return webhook is null ? null : Map(webhook);
        }

        public async Task<WebhookResponseDto?> UpdateAsync(int id, WebhookUpdateDto webhookUpdateDto, CancellationToken cancellationToken)
        {
            var webhook = await webhookRepository.FindByIdAsync(id, cancellationToken);
            if (webhook is null) return null;

            webhook.Url = webhookUpdateDto.Url.Trim();
            webhook.Ativo = webhookUpdateDto.Ativo;
            webhook.Secret = string.IsNullOrWhiteSpace(webhookUpdateDto.Secret) ? null : webhookUpdateDto.Secret;
            webhook.Eventos = ParseEventos(webhookUpdateDto.Eventos);
            webhook.AtualizadoEmUtc = DateTime.UtcNow;

            await webhookRepository.SaveChangesAsync(cancellationToken);
            return Map(webhook);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
        {
            var webhook = await webhookRepository.FindByIdAsync(id, cancellationToken);
            if (webhook is null) return false;

            await webhookRepository.RemoveAsync(webhook, cancellationToken);
            await webhookRepository.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> SendTestAsync(int id, WebhookTestPayloadDto payload, CancellationToken cancellationToken)
        {
            var webhook = await webhookRepository.GetByIdNoTrackingAsync(id, cancellationToken);
            if (webhook is null || !webhook.Ativo) return false;

            var envelope = new
            {
                id = Guid.NewGuid(),
                type = payload.Tipo,
                created_at = DateTime.UtcNow,
                data = payload.Dados
            };

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            using var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url)
            {
                Content = JsonContent.Create(envelope)
            };

            if (!string.IsNullOrWhiteSpace(webhook.Secret))
            {
                var body = System.Text.Json.JsonSerializer.Serialize(envelope);
                var sig = WebhookSigning.SignBodySha256(body, webhook.Secret!);
                request.Headers.Add("X-Versopay-Signature", $"sha256={sig}");
                request.Headers.Add("X-Versopay-Event", payload.Tipo);
            }

            try
            {
                var response = await http.SendAsync(request, cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Erro ao enviar webhook de teste para {Url}", webhook.Url);
                return false;
            }
        }

        private static WebhookEvent ParseEventos(IEnumerable<string> nomes)
        {
            WebhookEvent flags = WebhookEvent.None;
            foreach (var nome in nomes ?? Array.Empty<string>())
            {
                if (Enum.TryParse<WebhookEvent>(nome, ignoreCase: true, out var webhookEvent))
                    flags |= webhookEvent;
            }
            return flags;
        }

        private static WebhookResponseDto Map(WebhookModel webhook) => new()
        {
            Id = webhook.Id,
            Url = webhook.Url,
            Ativo = webhook.Ativo,
            HasSecret = !string.IsNullOrWhiteSpace(webhook.Secret),
            Eventos = ExpandEventos(webhook.Eventos),
            EventosMask = (int)webhook.Eventos,
            CriadoEmUtc = webhook.CriadoEmUtc,
            AtualizadoEmUtc = webhook.AtualizadoEmUtc
        };

        private static string[] ExpandEventos(WebhookEvent flags) =>
            Enum.GetValues<WebhookEvent>()
                .Where(webhookEvent => webhookEvent != WebhookEvent.None && flags.HasFlag(webhookEvent))
                .Select(webhookEvent => webhookEvent.ToString())
                .ToArray();
    }
}
