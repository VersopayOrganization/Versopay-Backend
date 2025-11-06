using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace VersopayBackend.Repositories
{
    public interface IVexyBankClient
    {
        // Garante que existe um JWT válido em cache e devolve a string do token
        Task<string> EnsureJwtAsync(int ownerUserId, CancellationToken ct);

        // Chamada GET autenticada (Bearer)
        Task<T> GetAsync<T>(int ownerUserId, string path, CancellationToken ct);

        // Chamada POST autenticada (Bearer)
        Task<TResp> PostAsync<TReq, TResp>(int ownerUserId, string path, TReq body, CancellationToken ct);

        // ✅ NOVO: com idempotency
        Task<TResp> PostAsync<TReq, TResp>(int ownerUserId, string path, TReq body, string idempotencyKey, CancellationToken ct);
    }
}
