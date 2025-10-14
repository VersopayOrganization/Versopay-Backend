// Repositories/VexyClient.cs
using System.Net.Http.Json;
using System.Text.Json;
using VersopayLibrary.Enums;
using VersopayBackend.Repositories;

public sealed class VexyClient(
    IHttpClientFactory httpFactory,
    IProviderCredentialRepository repo,
    ILogger<VexyClient> logger) : IVexyClient
{
    private HttpClient Create() => httpFactory.CreateClient("Vexy");

    public async Task<TResp> PostAsync<TReq, TResp>(int ownerUserId, string path, TReq body, CancellationToken ct)
    {
        var token = await GetAccessTokenAsync(ownerUserId, ct);
        var http = Create();
        using var req = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body)
        };
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var resp = await http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var data = await resp.Content.ReadFromJsonAsync<TResp>(cancellationToken: ct);
        if (data is null) throw new InvalidOperationException("Resposta vazia da Vexy.");
        return data;
    }

    public async Task<string> GetAccessTokenAsync(int ownerUserId, CancellationToken ct)
    {
        var cred = await repo.GetAsync(ownerUserId, PaymentProvider.Vexy, ct)
                  ?? throw new InvalidOperationException("Credenciais Vexy não configuradas para este usuário.");

        if (!string.IsNullOrWhiteSpace(cred.AccessToken) &&
            cred.AccessTokenExpiresUtc.HasValue &&
            cred.AccessTokenExpiresUtc.Value > DateTime.UtcNow.AddMinutes(1))
            return cred.AccessToken!;

        var http = Create();

        // TODO: confirme o path e os nomes das propriedades de login na Vexy
        var resp = await http.PostAsJsonAsync("/api/auth/login", new
        {
            client_id = cred.ClientId,
            client_secret = cred.ClientSecret
        }, ct);
        resp.EnsureSuccessStatusCode();

        // TODO: confirme o shape real da resposta
        var json = await resp.Content.ReadFromJsonAsync<VexyLoginResp>(cancellationToken: ct)
                   ?? throw new InvalidOperationException("Resposta de autenticação inválida Vexy.");

        cred.AccessToken = json.token;
        cred.AccessTokenExpiresUtc = DateTime.UtcNow.AddMinutes(50); // TODO: ajuste conforme TTL real do JWT
        await repo.AddOrUpdateAsync(cred, ct);
        await repo.SaveChangesAsync(ct);

        return cred.AccessToken!;
    }

    public async Task<HttpResponseMessage> PostAsync(int ownerUserId, string path, object body, CancellationToken ct)
    {
        var token = await GetAccessTokenAsync(ownerUserId, ct);
        var http = Create();
        using var req = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body)
        };
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return await http.SendAsync(req, ct);
    }

    public async Task<T> GetJsonAsync<T>(int ownerUserId, string path, CancellationToken ct)
    {
        var token = await GetAccessTokenAsync(ownerUserId, ct);
        var http = Create();
        using var req = new HttpRequestMessage(HttpMethod.Get, path);
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var resp = await http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var data = await resp.Content.ReadFromJsonAsync<T>(options, ct);
        if (data is null) throw new InvalidOperationException("Resposta vazia da Vexy.");
        return data;
    }

    private sealed record VexyLoginResp(string token); // TODO: adeque ao JSON real
}
