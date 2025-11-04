namespace VersopayBackend.Services.Vexy
{
    public interface IVexyBankClient
    {
        Task<string> GetTokenAsync(int ownerUserId, CancellationToken ct);
        Task<T> GetAsync<T>(int ownerUserId, string path, CancellationToken ct);
        Task<TResp> PostAsync<TReq, TResp>(int ownerUserId, string path, TReq body,
            CancellationToken ct, IDictionary<string, string>? extraHeaders = null);
    }
}
