using VersopayBackend.Dtos;
using VersopayBackend.Dtos.VexyBank;

namespace VersopayBackend.Services.Vexy
{
    public interface IVexyBankService
    {
        Task<PixInCreateRespDto> CreatePixInAsync(int ownerUserId, PixInCreateReqDto req, CancellationToken ct);
        Task<PixOutRespDto> SendPixOutAsync(int ownerUserId, PixOutReqDto req, string idempotencyKey, CancellationToken ct);

        // ✅ Consulta de status usa PixInStatusRespDto
        Task<PixInStatusRespDto> GetPixInAsync(int ownerUserId, string id, CancellationToken ct);

        Task<(bool ok, string? error)> ValidateCredentialsAsync(int ownerUserId, CancellationToken ct);
        Task HandleWebhookAsync(int ownerUserId, VexyWebhookEnvelope payload,
            string? sourceIp, IDictionary<string, string>? headers, CancellationToken ct);
    }
}
