namespace VersopayBackend.Repositories
{
    public interface IVersellClient
    {
        Task<HttpResponseMessage> PostAsync(int ownerUserId, string path, object body, CancellationToken ct);
    }
}
