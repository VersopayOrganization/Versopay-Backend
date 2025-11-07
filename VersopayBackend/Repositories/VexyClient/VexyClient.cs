// Repositories/VexyClient.cs
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using VersopayBackend.Repositories;
using VersopayLibrary.Enums;

namespace VersopayBackend.Repositories.Vexy
{
    public sealed class VexyClient(
        IHttpClientFactory httpFactory,
        IProviderCredentialRepository repo,
        ILogger<VexyClient> logger) : IVexyClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private HttpClient Create() => httpFactory.CreateClient("Vexy");

        // ------------------------- PUBLIC API -------------------------

        public async Task<TResp> PostAsync<TReq, TResp>(int ownerUserId, string path, TReq body, CancellationToken ct)
        {
            var http = Create();
            var token = await GetAccessTokenAsync(ownerUserId, ct);

            using var req = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(body) // respeita [JsonPropertyName]
            };
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var resp = await http.SendAsync(req, ct);

            // Se o token expirou e a Vexy respondeu 401, invalida e tenta 1x
            if (resp.StatusCode == HttpStatusCode.Unauthorized)
            {
                await InvalidateToken(ownerUserId, ct);
                token = await GetAccessTokenAsync(ownerUserId, ct);

                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                resp = await http.SendAsync(req, ct);
            }

            var raw = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"Vexy POST {path} failed: {(int)resp.StatusCode} {resp.ReasonPhrase} - {raw}");

            var data = JsonSerializer.Deserialize<TResp>(raw, JsonOptions);
            if (data is null) throw new InvalidOperationException("Resposta vazia da Vexy.");
            return data;
        }

        public async Task<HttpResponseMessage> PostAsync(int ownerUserId, string path, object body, CancellationToken ct)
        {
            var http = Create();
            var token = await GetAccessTokenAsync(ownerUserId, ct);

            using var req = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(body)
            };
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var resp = await http.SendAsync(req, ct);
            if (resp.StatusCode == HttpStatusCode.Unauthorized)
            {
                await InvalidateToken(ownerUserId, ct);
                token = await GetAccessTokenAsync(ownerUserId, ct);

                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                resp = await http.SendAsync(req, ct);
            }

            return resp;
        }

        public async Task<T> GetJsonAsync<T>(int ownerUserId, string path, CancellationToken ct)
        {
            var http = Create();
            var token = await GetAccessTokenAsync(ownerUserId, ct);

            using var req = new HttpRequestMessage(HttpMethod.Get, path);
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var resp = await http.SendAsync(req, ct);
            if (resp.StatusCode == HttpStatusCode.Unauthorized)
            {
                await InvalidateToken(ownerUserId, ct);
                token = await GetAccessTokenAsync(ownerUserId, ct);

                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                resp = await http.SendAsync(req, ct);
            }

            var raw = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"Vexy GET {path} failed: {(int)resp.StatusCode} {resp.ReasonPhrase} - {raw}");

            var data = JsonSerializer.Deserialize<T>(raw, JsonOptions);
            if (data is null) throw new InvalidOperationException("Resposta vazia da Vexy.");
            return data;
        }

        public async Task<string> GetAccessTokenAsync(int ownerUserId, CancellationToken ct)
        {
            var cred = await repo.GetAsync(ownerUserId, PaymentProvider.Vexy, ct)
                      ?? throw new InvalidOperationException("Credenciais Vexy não configuradas para este usuário.");

            // Token ainda válido?
            if (!string.IsNullOrWhiteSpace(cred.AccessToken) &&
                cred.AccessTokenExpiresUtc.HasValue &&
                cred.AccessTokenExpiresUtc.Value > DateTime.UtcNow.AddMinutes(1))
            {
                return cred.AccessToken!;
            }

            // Tenta autenticar (suporta variação de endpoint)
            var http = Create();

            // 1) payload padrão client_id/client_secret
            var loginBody = new { client_id = cred.ClientId, client_secret = cred.ClientSecret };

            // Alguns ambientes usam "/auth/login" e outros "/api/auth/login"
            var endpoints = new[] { "/api/auth/login", "/auth/login" };

            VexyLoginParsed? parsed = null;
            Exception? lastErr = null;

            foreach (var ep in endpoints)
            {
                try
                {
                    var resp = await http.PostAsJsonAsync(ep, loginBody, JsonOptions, ct);
                    var raw = await resp.Content.ReadAsStringAsync(ct);

                    if (!resp.IsSuccessStatusCode)
                        throw new InvalidOperationException($"Vexy auth failed: {(int)resp.StatusCode} {resp.ReasonPhrase} - {raw}");

                    parsed = ParseLogin(raw);
                    if (parsed is not null) break;
                }
                catch (Exception ex)
                {
                    lastErr = ex;
                    logger.LogWarning(ex, "Falha no endpoint de login Vexy {Endpoint}", ep);
                }
            }

            if (parsed is null)
            {
                logger.LogError(lastErr, "Falha ao autenticar na Vexy (todos endpoints testados).");
                throw new InvalidOperationException("Não foi possível autenticar na Vexy.");
            }

            cred.AccessToken = parsed.Token;
            cred.AccessTokenExpiresUtc = parsed.ExpiresAtUtc ?? DateTime.UtcNow.AddMinutes(50); // fallback 50min
            await repo.AddOrUpdateAsync(cred, ct);
            await repo.SaveChangesAsync(ct);

            logger.LogInformation("Token Vexy atualizado. Expira em {ExpUTC}", cred.AccessTokenExpiresUtc);
            return cred.AccessToken!;
        }

        // ------------------------- HELPERS -------------------------

        private async Task InvalidateToken(int ownerUserId, CancellationToken ct)
        {
            var cred = await repo.GetAsync(ownerUserId, PaymentProvider.Vexy, ct);
            if (cred is null) return;

            cred.AccessToken = null;
            cred.AccessTokenExpiresUtc = null;
            await repo.AddOrUpdateAsync(cred, ct);
            await repo.SaveChangesAsync(ct);
            logger.LogInformation("Token Vexy invalidado para OwnerUserId={Owner}", ownerUserId);
        }

        /// <summary>
        /// Tenta entender formatos comuns: {"token":"...","expires_in":3600} ou {"access_token":"..."} etc.
        /// </summary>
        private static VexyLoginParsed? ParseLogin(string raw)
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;

                string? token = null;
                DateTime? exp = null;

                // campos alternativos
                if (root.TryGetProperty("token", out var p1)) token = p1.GetString();
                if (token is null && root.TryGetProperty("access_token", out var p2)) token = p2.GetString();
                if (token is null && root.TryGetProperty("jwt", out var p3)) token = p3.GetString();

                if (root.TryGetProperty("expires_in", out var pin))
                {
                    if (pin.ValueKind == JsonValueKind.Number && pin.TryGetInt32(out var seconds))
                        exp = DateTime.UtcNow.AddSeconds(seconds);
                }
                else if (root.TryGetProperty("expiresAt", out var pexp) && pexp.ValueKind is JsonValueKind.String)
                {
                    if (DateTime.TryParse(pexp.GetString(), out var parsed))
                        exp = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
                }

                if (string.IsNullOrWhiteSpace(token)) return null;
                return new VexyLoginParsed(token!, exp);
            }
            catch
            {
                return null;
            }
        }

        private sealed record VexyLoginParsed(string Token, DateTime? ExpiresAtUtc);
    }
}
