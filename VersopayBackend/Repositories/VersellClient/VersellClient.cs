using VersopayLibrary.Enums;

namespace VersopayBackend.Repositories
{
    public sealed class VersellClient(
    IHttpClientFactory httpFactory,
    IProviderCredentialRepository repo) : IVersellClient
    {
        private HttpClient Create() => httpFactory.CreateClient("Versell"); // base: https://api.versellpay.com

        public async Task<HttpResponseMessage> PostAsync(int ownerUserId, string path, object body, CancellationToken ct)
        {
            var cred = await repo.GetAsync(ownerUserId, PaymentProvider.Versell, ct)
                      ?? throw new InvalidOperationException("Credenciais Versell não configuradas para este usuário.");

            var http = Create();
            using var req = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(body)
            };
            req.Headers.Add("vspi", cred.ClientId);
            req.Headers.Add("vsps", cred.ClientSecret);

            return await http.SendAsync(req, ct);
        }
    }
}
