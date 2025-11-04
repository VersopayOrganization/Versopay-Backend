using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using VersopayBackend.Repositories;
using VersopayLibrary.Enums;

namespace VersopayBackend.Services.Vexy
{
    public sealed class VexyBankClient(
    IHttpClientFactory httpFactory,
    IProviderCredentialRepository credRepo,
    ILogger<VexyBankClient> logger) : IVexyBankClient
    {
        static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
        HttpClient Create() => httpFactory.CreateClient("VexyBank");

        public async Task<string> GetTokenAsync(int ownerUserId, CancellationToken ct)
        {
            var cred = await credRepo.GetAsync(ownerUserId, PaymentProvider.Vexy, ct)
                       ?? throw new InvalidOperationException("Credenciais Vexy não configuradas para este usuário.");
            if (string.IsNullOrWhiteSpace(cred.ApiKey) || string.IsNullOrWhiteSpace(cred.ApiSecret))
                throw new InvalidOperationException("API Key/Secret não configuradas para este usuário (Vexy).");

            if (!string.IsNullOrWhiteSpace(cred.AccessToken) &&
                cred.AccessTokenExpiresUtc.HasValue &&
                cred.AccessTokenExpiresUtc.Value > DateTime.UtcNow.AddMinutes(1))
                return cred.AccessToken!;

            var http = Create();
            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{cred.ApiKey}:{cred.ApiSecret}"));

            using var req = new HttpRequestMessage(HttpMethod.Post, "/api/auth");
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

            var resp = await http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"Auth Vexy falhou: {(int)resp.StatusCode} {resp.ReasonPhrase} - {raw}");

            var json = JsonSerializer.Deserialize<AuthResp>(raw, _json)
                       ?? throw new InvalidOperationException("Resposta de auth inválida.");

            cred.AccessToken = json.token;
            // expiresIn vem em milissegundos; subtraia 60s de margem
            var ms = Math.Max(0, json.expiresIn - 60_000);
            cred.AccessTokenExpiresUtc = DateTime.UtcNow.AddMilliseconds(ms);
            await credRepo.AddOrUpdateAsync(cred, ct);
            await credRepo.SaveChangesAsync(ct);

            return cred.AccessToken!;
        }

        public async Task<T> GetAsync<T>(int ownerUserId, string path, CancellationToken ct)
        {
            var token = await GetTokenAsync(ownerUserId, ct);
            var http = Create();
            using var req = new HttpRequestMessage(HttpMethod.Get, path);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var resp = await http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"GET {path} falhou: {(int)resp.StatusCode} {resp.ReasonPhrase} - {raw}");

            return JsonSerializer.Deserialize<T>(raw, _json) ?? throw new InvalidOperationException("Resposta vazia.");
        }

        public async Task<TResp> PostAsync<TReq, TResp>(int ownerUserId, string path, TReq body,
            CancellationToken ct, IDictionary<string, string>? extraHeaders = null)
        {
            var token = await GetTokenAsync(ownerUserId, ct);
            var http = Create();

            using var req = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(body)
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (extraHeaders != null)
                foreach (var (k, v) in extraHeaders) req.Headers.TryAddWithoutValidation(k, v);

            var resp = await http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"POST {path} falhou: {(int)resp.StatusCode} {resp.ReasonPhrase} - {raw}");

            return JsonSerializer.Deserialize<TResp>(raw, _json) ?? throw new InvalidOperationException("Resposta vazia.");
        }

        private sealed record AuthResp(bool success, string token, string tokenType, int expiresIn);
    }
}
