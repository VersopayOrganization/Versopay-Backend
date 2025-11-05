using VersopayBackend.Dtos;
using VersopayBackend.Dtos.VexyBank;

namespace VersopayBackend.Repositories.VexyClient
{
    public interface IVexyBankService
    {
        Task<PixInCreateRespDto> CreatePixInAsync(int ownerUserId, PixInCreateReqDto req, CancellationToken ct);
        Task<PixOutRespDto> SendPixOutAsync(int ownerUserId, PixOutReqDto req, string idempotencyKey, CancellationToken ct);
        // ✅ novo
        Task<PixInStatusRespDto> GetPixInAsync(int ownerUserId, string id, CancellationToken ct);

        // Webhook ingest (multi-tenant)
        Task HandleWebhookAsync(int ownerUserId, Dtos.VexyBank.VexyWebhookEnvelope payload,
            string? sourceIp, IDictionary<string, string>? headers, CancellationToken ct);
    }
}
