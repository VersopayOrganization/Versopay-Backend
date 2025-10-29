// IInboundWebhookService.cs
using VersopayBackend.Dtos;
using VersopayLibrary.Enums;

namespace VersopayBackend.Services
{
    public interface IInboundWebhookService
    {
        Task<ProcessingStatus> HandleVersellAsync(
            VersellWebhookDto versellWebhookDto, string sourceIp, IDictionary<string, string> headers, CancellationToken cancellationToken);

        Task<ProcessingStatus> HandleVexyAsync(
            VexyWebhookDto vexyWebhookDto, string sourceIp, IDictionary<string, string> headers, CancellationToken cancellationToken);
    }
}
