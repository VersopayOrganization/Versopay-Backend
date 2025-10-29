// VersopayBackend/Services/Webhooks/IWebhooksService.cs
using VersopayBackend.Dtos;

namespace VersopayBackend.Services
{
    public interface IWebhooksService
    {
        Task<WebhookResponseDto> CreateAsync(WebhookCreateDto webhookCreateDto, CancellationToken cancellationToken);
        Task<IEnumerable<WebhookResponseDto>> GetAllAsync(bool? ativo, CancellationToken cancellationToken);
        Task<WebhookResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<WebhookResponseDto?> UpdateAsync(int id, WebhookUpdateDto webhookUpdateDto, CancellationToken cancellationToken);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
        Task<bool> SendTestAsync(int id, WebhookTestPayloadDto webhookTestPayloadDto, CancellationToken cancellationToken);
    }
}
