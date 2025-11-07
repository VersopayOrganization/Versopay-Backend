using VersopayLibrary.Models;

namespace VersopayBackend.Dtos.Common
{
    public interface IVendorWebhookHandler
    {
        Task<bool> VerifySignatureAsync(int ownerUserId, string rawBody, IDictionary<string, string> headers, CancellationToken ct);
        string BuildEventKey(string rawBody, IDictionary<string, string> headers);
        Task<InboundWebhookLog> ProcessAsync(int ownerUserId, string channel, string rawBody, IDictionary<string, string> headers, string sourceIp, CancellationToken ct);
    }

}
