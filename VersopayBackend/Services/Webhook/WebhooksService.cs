using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using VersopayBackend.Dtos;
using VersopayBackend.Repositories;
using VersopayBackend.Utils;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;
using System.Security.Claims;

namespace VersopayBackend.Services
{
    public sealed class WebhooksService(
        IWebhookRepository webhookRepository,
        IHttpContextAccessor httpContextAccessor,
        ILogger<WebhooksService> logger
    ) : IWebhooksService
    {
        private ClaimsPrincipal User => httpContextAccessor.HttpContext!.User;

        private static int CurrentUserId(ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true)
                throw new UnauthorizedAccessException("Requer autenticação.");

            // tenta "sub" (quando MapInboundClaims = false) e depois NameIdentifier (padrão do ASP.NET)
            var idStr = user.FindFirst("sub")?.Value
                     ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(idStr) || !int.TryParse(idStr, out var id))
                throw new UnauthorizedAccessException("ID do usuário não encontrado no token.");

            return id;
        }

        private static bool IsAdmin(ClaimsPrincipal user)
        {
            // você grava "isAdmin": "true/false" no token
            var raw = user.FindFirst("isAdmin")?.Value;
            return bool.TryParse(raw, out var b) && b || user.IsInRole("Admin");
        }

        public async Task<WebhookResponseDto> CreateAsync(WebhookCreateDto dto, CancellationToken ct)
        {
            var ownerId = CurrentUserId(User);   // agora seguro
            var eventos = ParseEventos(dto.Eventos);

            var entity = new Webhook
            {
                Url = dto.Url.Trim(),
                Ativo = dto.Ativo,
                Secret = string.IsNullOrWhiteSpace(dto.Secret) ? null : dto.Secret,
                Eventos = eventos,
                OwnerUserId = ownerId,
                CriadoEmUtc = DateTime.UtcNow
            };

            await webhookRepository.AddAsync(entity, ct);
            await webhookRepository.SaveChangesAsync(ct);
            return Map(entity);
        }

        public async Task<IEnumerable<WebhookResponseDto>> GetAllAsync(bool? ativo, CancellationToken ct)
        {
            var user = User;
            var isAdmin = IsAdmin(user);
            var ownerId = CurrentUserId(user);

            var q = webhookRepository.QueryNoTracking();

            if (!isAdmin)
                q = q.Where(w => w.OwnerUserId == ownerId);

            if (ativo.HasValue)
                q = q.Where(w => w.Ativo == ativo.Value);

            var list = await q.OrderByDescending(x => x.CriadoEmUtc).ToListAsync(ct);
            return list.Select(Map);
        }

        public async Task<WebhookResponseDto?> GetByIdAsync(int id, CancellationToken ct)
        {
            var user = User;
            var isAdmin = IsAdmin(user);
            var ownerId = CurrentUserId(user);

            var wh = await webhookRepository.GetByIdNoTrackingAsync(id, ct);
            if (wh is null) return null;

            if (!isAdmin && wh.OwnerUserId != ownerId)
                return null; // 404 pra quem não é dono

            return Map(wh);
        }

        public async Task<WebhookResponseDto?> UpdateAsync(int id, WebhookUpdateDto dto, CancellationToken ct)
        {
            var user = User;
            var isAdmin = IsAdmin(user);
            var ownerId = CurrentUserId(user);

            var wh = await webhookRepository.FindByIdAsync(id, ct);
            if (wh is null) return null;

            if (!isAdmin && wh.OwnerUserId != ownerId)
                return null;

            wh.Url = dto.Url.Trim();
            wh.Ativo = dto.Ativo;
            wh.Secret = string.IsNullOrWhiteSpace(dto.Secret) ? null : dto.Secret;
            wh.Eventos = ParseEventos(dto.Eventos);
            wh.AtualizadoEmUtc = DateTime.UtcNow;

            await webhookRepository.SaveChangesAsync(ct);
            return Map(wh);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct)
        {
            var user = User;
            var isAdmin = IsAdmin(user);
            var ownerId = CurrentUserId(user);

            var wh = await webhookRepository.FindByIdAsync(id, ct);
            if (wh is null) return false;

            if (!isAdmin && wh.OwnerUserId != ownerId)
                return false;

            await webhookRepository.RemoveAsync(wh, ct);
            await webhookRepository.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> SendTestAsync(int id, WebhookTestPayloadDto payload, CancellationToken ct)
        {
            var user = User;
            var isAdmin = IsAdmin(user);
            var ownerId = CurrentUserId(user);

            var wh = await webhookRepository.GetByIdNoTrackingAsync(id, ct);
            if (wh is null || !wh.Ativo) return false;

            if (!isAdmin && wh.OwnerUserId != ownerId)
                return false;

            var envelope = new
            {
                id = Guid.NewGuid(),
                type = payload.Tipo,
                created_at = DateTime.UtcNow,
                data = payload.Dados
            };

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            using var req = new HttpRequestMessage(HttpMethod.Post, wh.Url)
            {
                Content = JsonContent.Create(envelope)
            };

            if (!string.IsNullOrWhiteSpace(wh.Secret))
            {
                var body = System.Text.Json.JsonSerializer.Serialize(envelope);
                var sig = WebhookSigning.SignBodySha256(body, wh.Secret!);
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
                logger.LogWarning(ex, "Erro ao enviar webhook de teste para {Url}", wh.Url);
                return false;
            }
        }

        // helpers de eventos e mapping (iguais aos seus)
        private static WebhookEvent ParseEventos(IEnumerable<string> nomes)
        {
            WebhookEvent flags = WebhookEvent.None;
            foreach (var n in nomes ?? Array.Empty<string>())
                if (Enum.TryParse<WebhookEvent>(n, true, out var ev)) flags |= ev;
            return flags;
        }

        private static WebhookResponseDto Map(Webhook w) => new()
        {
            Id = w.Id,
            Url = w.Url,
            Ativo = w.Ativo,
            HasSecret = !string.IsNullOrWhiteSpace(w.Secret),
            Eventos = Enum.GetValues<WebhookEvent>()
                          .Where(v => v != WebhookEvent.None && w.Eventos.HasFlag(v))
                          .Select(v => v.ToString())
                          .ToArray(),
            EventosMask = (int)w.Eventos,
            CriadoEmUtc = w.CriadoEmUtc,
            AtualizadoEmUtc = w.AtualizadoEmUtc,
            OwnerUserId = w.OwnerUserId
        };
    }
}
