using System.Text.Json;
using System.Linq; // Where(char.IsDigit)
using VersopayBackend.Dtos;
using VersopayBackend.Dtos.VexyBank;
using VersopayBackend.Repositories;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Services.Vexy
{
    public sealed class VexyBankService(
        IVexyBankClient client,
        IInboundWebhookLogRepository inboundLogRepo,
        IProviderCredentialRepository credRepo,
        ILogger<VexyBankService> logger
    ) : IVexyBankService
    {
        private readonly IVexyBankClient _client = client;
        private readonly IInboundWebhookLogRepository _inboundLogRepo = inboundLogRepo;
        private readonly IProviderCredentialRepository _credRepo = credRepo;
        private readonly ILogger<VexyBankService> _logger = logger;

        private static string BuildUserWebhookUrl(string publicBaseUrl, int ownerUserId, string channel) =>
            $"{publicBaseUrl.TrimEnd('/')}/api/webhooks/v1/vexy/{ownerUserId}/{channel}";

        public async Task<(bool ok, string? error)> ValidateCredentialsAsync(int ownerUserId, CancellationToken ct)
        {
            try
            {
                // 1) precisa existir o registro
                var cred = await _credRepo.GetAsync(ownerUserId, PaymentProvider.Vexy, ct);
                if (cred is null)
                    return (false, "Credenciais não encontradas para este usuário.");

                // 2) precisa ter ApiKey e ApiSecret (public/secret)
                if (string.IsNullOrWhiteSpace(cred.ApiKey) || string.IsNullOrWhiteSpace(cred.ApiSecret))
                    return (false, "ApiKey (public) e ApiSecret (secret) não foram informadas.");

                // 3) Tenta gerar um JWT via Basic (apiKey:apiSecret).
                //    Se conseguir, as credenciais estão ok.
                var token = await _client.EnsureJwtAsync(ownerUserId, ct);
                return string.IsNullOrWhiteSpace(token)
                    ? (false, "Não foi possível obter token JWT na VexyBank.")
                    : (true, null);
            }
            catch (OperationCanceledException)
            {
                return (false, "Operação cancelada.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar credenciais VexyBank (OwnerUserId={Owner})", ownerUserId);
                return (false, ex.Message);
            }
        }

        public async Task<PixInCreateRespDto> CreatePixInAsync(int ownerUserId, PixInCreateReqDto req, CancellationToken ct)
        {
            // garante que há credenciais salvas
            _ = await _credRepo.GetAsync(ownerUserId, PaymentProvider.Vexy, ct)
                ?? throw new InvalidOperationException("Credenciais Vexy não configuradas para este usuário.");

            // postback padrão caso não venha
            if (string.IsNullOrWhiteSpace(req.PostbackUrl))
            {
                var publicBase = Environment.GetEnvironmentVariable("PUBLIC_BASE_URL")
                                 ?? "https://versopay.com.br";
                req.PostbackUrl = BuildUserWebhookUrl(publicBase, ownerUserId, "pix-in");
            }

            // normalizações
            req.Customer.Document = new string((req.Customer.Document ?? "").Where(char.IsDigit).ToArray());
            req.Customer.DocumentType = req.Customer.Document?.Length == 14 ? "cnpj" : "cpf";

            // Aqui o client injeta Bearer (obtido via EnsureJwtAsync)
            var resp = await _client.PostAsync<PixInCreateReqDto, PixInCreateRespDto>(
                ownerUserId, "/api/v1/pix/in/qrcode", req, ct);

            return resp;
        }

        public async Task<PixOutRespDto> SendPixOutAsync(int ownerUserId, PixOutReqDto req, string idempotencyKey, CancellationToken ct)
        {
            _ = await _credRepo.GetAsync(ownerUserId, PaymentProvider.Vexy, ct)
                ?? throw new InvalidOperationException("Credenciais Vexy não configuradas para este usuário.");

            if (string.IsNullOrWhiteSpace(req.PostbackUrl))
            {
                var publicBase = Environment.GetEnvironmentVariable("PUBLIC_BASE_URL")
                                 ?? "https://versopay.com.br";
                req.PostbackUrl = BuildUserWebhookUrl(publicBase, ownerUserId, "pix-out");
            }

            // TODO: ideal enviar "x-idempotency-key" como header. Seu IVexyBankClient atual
            // não possui sobrecarga com headers; quando criarmos, injete o header aqui.
            var resp = await _client.PostAsync<PixOutReqDto, PixOutRespDto>(
                ownerUserId, "/api/v1/pix/out/pixkey", req, ct);

            return resp;
        }

        public async Task<PixInCreateRespDto> GetPixInAsync(int ownerUserId, string id, CancellationToken ct)
        {
            // garante credenciais e JWT
            _ = await _credRepo.GetAsync(ownerUserId, PaymentProvider.Vexy, ct)
                ?? throw new InvalidOperationException("Credenciais Vexy não configuradas para este usuário.");
            // EnsureJwtAsync é chamado dentro do client nos GET/POST.

            // Candidatos de rota mais comuns encontrados em gateways Pix:
            var candidates = new[]
            {
        $"/api/v1/pix/in/{id}",
        $"/api/v1/pix/in/qrcode/{id}",
        $"/api/v1/pix/in/{id}/qrcode"
    };

            Exception? lastEx = null;

            foreach (var path in candidates)
            {
                try
                {
                    // Se a rota não existir, seu VexyBankClient deve lançar InvalidOperationException
                    // com 404 — nós capturamos e tentamos o próximo candidato.
                    var resp = await _client.GetAsync<PixInCreateRespDto>(ownerUserId, path, ct);
                    if (resp is not null) return resp;
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("404 Not Found", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("VexyBank GET {Path} retornou 404, tentando próximo candidato…", path);
                    lastEx = ex;
                    continue;
                }
            }

            // Se nenhum funcionou, devolve o último erro para depuração rápida.
            throw new InvalidOperationException(
                $"Nenhuma rota de consulta PixIn funcionou para id '{id}'. " +
                $"Tente conferir a documentação do provedor e ajuste o path no serviço.",
                lastEx
            );
        }

        public async Task HandleWebhookAsync(
            int ownerUserId,
            VexyWebhookEnvelope payload,
            string? sourceIp,
            IDictionary<string, string>? headers,
            CancellationToken ct)
        {
            // dedupe por id+tipo+owner
            var eventKey = $"vexy:{ownerUserId}:{payload.Type}:{payload.Event}:{payload.Id}".ToLowerInvariant();
            if (await _inboundLogRepo.ExistsByEventKeyAsync(eventKey, ct)) return;

            var log = new InboundWebhookLog
            {
                Provedor = ProvedorWebhook.VexyPayments,
                Evento = payload.Type?.Equals("transaction", StringComparison.OrdinalIgnoreCase) == true
                           ? WebhookEvento.PagamentoPIX
                           : WebhookEvento.TransferenciaPIX,
                EventKey = eventKey,
                SourceIp = sourceIp ?? "",
                HeadersJson = JsonSerializer.Serialize(headers ?? new Dictionary<string, string>()),
                PayloadJson = JsonSerializer.Serialize(payload),
                ReceivedAtUtc = DateTime.UtcNow,
                ProcessedAtUtc = DateTime.UtcNow,
                ProcessingStatus = ProcessingStatus.Success,
                TransactionId = payload.Transaction?.Id ?? payload.Transfer?.Id,
                Status = payload.Transaction?.Status ?? payload.Transfer?.Status,
                DataEventoUtc = DateTime.UtcNow
            };

            await _inboundLogRepo.AddAsync(log, ct);
            await _inboundLogRepo.SaveChangesAsync(ct);

            // aqui você faz os efeitos de domínio (aprovar pedido, atualizar extrato, etc.)
        }
    }
}
