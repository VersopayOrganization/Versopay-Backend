// VersopayBackend/Services/Webhooks/IWebhooksService.cs
using VersopayBackend.Dtos;

namespace VersopayBackend.Services.Webhooks
{
    public interface IWebhooksService
    {
        Task<WebhookResponseDto> CreateAsync(WebhookCreateDto dto, CancellationToken ct);
        Task<IEnumerable<WebhookResponseDto>> GetAllAsync(bool? ativo, CancellationToken ct);
        Task<WebhookResponseDto?> GetByIdAsync(int id, CancellationToken ct);
        Task<WebhookResponseDto?> UpdateAsync(int id, WebhookUpdateDto dto, CancellationToken ct);
        Task<bool> DeleteAsync(int id, CancellationToken ct);
        Task<bool> SendTestAsync(int id, WebhookTestPayloadDto payload, CancellationToken ct);
    }
}
