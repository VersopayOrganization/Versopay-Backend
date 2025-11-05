using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using VersopayBackend.Repositories;
using VersopayLibrary.Enums;

namespace VersopayBackend.Repositories.Vexy
{
    public sealed class VexyBankClient : IVexyBankClient
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly IProviderCredentialRepository _credRepo;
        private readonly ILogger<VexyBankClient> _logger;

        public VexyBankClient(
            IHttpClientFactory httpFactory,
            IProviderCredentialRepository credRepo,
            ILogger<VexyBankClient> logger)
        {
            _httpFactory = httpFactory;
            _credRepo = credRepo;
            _logger = logger;
        }

        private HttpClient Create() => _httpFactory.CreateClient("VexyBank");

        private static string ToBasic(string apiKey, string apiSecret)
            => Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:{apiSecret}"));

        private sealed record AuthResp(string token, int expires_in);

        public async Task<string> EnsureJwtAsync(int ownerUserId, CancellationToken ct)
        {
            var cred = await _credRepo.GetAsync(ownerUserId, PaymentProvider.Vexy, ct)
                       ?? throw new InvalidOperationException("Credenciais Vexy não configuradas para este usuário.");

            if (string.IsNullOrWhiteSpace(cred.ApiKey) || string.IsNullOrWhiteSpace(cred.ApiSecret))
                throw new InvalidOperationException("ApiKey/ApiSecret não configuradas para este usuário.");

            // Se já existe e não vai expirar em 1 min, reaproveita
            if (!string.IsNullOrWhiteSpace(cred.AccessToken) &&
                cred.AccessTokenExpiresUtc.HasValue &&
                cred.AccessTokenExpiresUtc.Value > DateTime.UtcNow.AddMinutes(1))
            {
                return cred.AccessToken!;
            }

            var http = Create();

            using var req = new HttpRequestMessage(HttpMethod.Post, "/api/auth");
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", ToBasic(cred.ApiKey!, cred.ApiSecret!));
            // Sem body (a rota /api/auth NÃO aceita body)

            var resp = await http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("VexyBank auth falhou: {Status} - {Body}", (int)resp.StatusCode, raw);
                throw new InvalidOperationException($"VexyBank auth failed: {(int)resp.StatusCode} - {raw}");
            }

            var data = JsonSerializer.Deserialize<AuthResp>(raw, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("Resposta de auth inválida (vazia).");

            // Guarda token no BD (colunas já existem no seu modelo)
            cred.AccessToken = data.token;
            // margem de segurança de 30s
            cred.AccessTokenExpiresUtc = DateTime.UtcNow.AddSeconds(Math.Max(0, data.expires_in - 30));
            await _credRepo.AddOrUpdateAsync(cred, ct);
            await _credRepo.SaveChangesAsync(ct);

            return cred.AccessToken!;
        }

        public async Task<T> GetAsync<T>(int ownerUserId, string path, CancellationToken ct)
        {
            var token = await EnsureJwtAsync(ownerUserId, ct);
            var http = Create();

            using var req = new HttpRequestMessage(HttpMethod.Get, path);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var resp = await http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"VexyBank GET {path} failed: {(int)resp.StatusCode} {resp.ReasonPhrase} - {raw}");

            var data = JsonSerializer.Deserialize<T>(raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (data is null) throw new InvalidOperationException($"VexyBank GET {path} retornou vazio.");
            return data;
        }

        public async Task<TResp> PostAsync<TReq, TResp>(int ownerUserId, string path, TReq body, CancellationToken ct)
        {
            var token = await EnsureJwtAsync(ownerUserId, ct);
            var http = Create();

            using var req = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(body)
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var resp = await http.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"VexyBank POST {path} failed: {(int)resp.StatusCode} {resp.ReasonPhrase} - {raw}");

            var data = JsonSerializer.Deserialize<TResp>(raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (data is null) throw new InvalidOperationException($"VexyBank POST {path} retornou vazio.");
            return data;
        }
    }
}
