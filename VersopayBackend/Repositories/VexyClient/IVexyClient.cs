namespace VersopayBackend.Repositories
{
    // Repositories/IVexyClient.cs
    public interface IVexyClient
    {
        Task<string> GetAccessTokenAsync(int ownerUserId, CancellationToken ct);
        Task<HttpResponseMessage> PostAsync(int ownerUserId, string path, object body, CancellationToken ct);
        Task<T> GetJsonAsync<T>(int ownerUserId, string path, CancellationToken ct);
        Task<TResp> PostAsync<TReq, TResp>(int ownerUserId, string path, TReq body, CancellationToken ct);
    }

}
