
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Net.Http.Json;
using VersopayBackend.Dtos;
using VersopayBackend.Repositories;
using VersopayBackend.Repositories.Webhook;
using VersopayBackend.Utils;
using VersopayLibrary.Enums;
using WebhookModel = VersopayLibrary.Models.Webhook; 

namespace VersopayBackend.Services.Webhooks
{
    public sealed class WebhooksService(IWebhookRepository repo, ILogger<WebhooksService> logger) : IWebhooksService
    {
        public async Task<WebhookResponseDto> CreateAsync(WebhookCreateDto dto, CancellationToken ct)
        {
            var eventos = ParseEventos(dto.Eventos);

            var entity = new WebhookModel
            {
                Url = dto.Url.Trim(),
                Ativo = dto.Ativo,
                Secret = string.IsNullOrWhiteSpace(dto.Secret) ? null : dto.Secret,
                Eventos = eventos,
                CriadoEmUtc = DateTime.UtcNow
            };

            await repo.AddAsync(entity, ct);
            await repo.SaveChangesAsync(ct);

            return Map(entity);
        }

        public async Task<IEnumerable<WebhookResponseDto>> GetAllAsync(bool? ativo, CancellationToken ct)
        {
            var q = repo.QueryNoTracking().OrderByDescending(x => x.CriadoEmUtc);
            if (ativo.HasValue)
                q = q.Where(x => x.Ativo == ativo.Value).OrderByDescending(x => x.CriadoEmUtc);

            var list = await q.ToListAsync(ct);
            return list.Select(Map);
        }

        public async Task<WebhookResponseDto?> GetByIdAsync(int id, CancellationToken ct)
        {
            var e = await repo.GetByIdNoTrackingAsync(id, ct);
            return e is null ? null : Map(e);
        }

        public async Task<WebhookResponseDto?> UpdateAsync(int id, WebhookUpdateDto dto, CancellationToken ct)
        {
            var e = await repo.FindByIdAsync(id, ct);
            if (e is null) return null;

            e.Url = dto.Url.Trim();
            e.Ativo = dto.Ativo;
            e.Secret = string.IsNullOrWhiteSpace(dto.Secret) ? null : dto.Secret;
            e.Eventos = ParseEventos(dto.Eventos);
            e.AtualizadoEmUtc = DateTime.UtcNow;

            await repo.SaveChangesAsync(ct);
            return Map(e);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct)
        {
            var e = await repo.FindByIdAsync(id, ct);
            if (e is null) return false;

            await repo.RemoveAsync(e, ct);
            await repo.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> SendTestAsync(int id, WebhookTestPayloadDto payload, CancellationToken ct)
        {
            var e = await repo.GetByIdNoTrackingAsync(id, ct);
            if (e is null || !e.Ativo) return false;

            var envelope = new
            {
                id = Guid.NewGuid(),
                type = payload.Tipo,
                created_at = DateTime.UtcNow,
                data = payload.Dados
            };

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            using var req = new HttpRequestMessage(HttpMethod.Post, e.Url)
            {
                Content = JsonContent.Create(envelope)
            };

            if (!string.IsNullOrWhiteSpace(e.Secret))
            {
                var body = System.Text.Json.JsonSerializer.Serialize(envelope);
                var sig = WebhookSigning.SignBodySha256(body, e.Secret!);
                req.Headers.Add("X-Versopay-Signature", $"sha256={sig}");
                req.Headers.Add("X-Versopay-Event", payload.Tipo);
            }

            try
            {
                var resp = await http.SendAsync(req, ct);
                return resp.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Erro ao enviar webhook de teste para {Url}", e.Url);
                return false;
            }
        }

        private static WebhookEvent ParseEventos(IEnumerable<string> nomes)
        {
            WebhookEvent flags = WebhookEvent.None;
            foreach (var n in nomes ?? Array.Empty<string>())
            {
                if (Enum.TryParse<WebhookEvent>(n, ignoreCase: true, out var ev))
                    flags |= ev;
            }
            return flags;
        }

        private static WebhookResponseDto Map(WebhookModel e) => new()
        {
            Id = e.Id,
            Url = e.Url,
            Ativo = e.Ativo,
            HasSecret = !string.IsNullOrWhiteSpace(e.Secret),
            Eventos = ExpandEventos(e.Eventos),
            EventosMask = (int)e.Eventos,
            CriadoEmUtc = e.CriadoEmUtc,
            AtualizadoEmUtc = e.AtualizadoEmUtc
        };

        private static string[] ExpandEventos(WebhookEvent flags) =>
            Enum.GetValues<WebhookEvent>()
                .Where(v => v != WebhookEvent.None && flags.HasFlag(v))
                .Select(v => v.ToString())
                .ToArray();
    }
}
