// VersopayBackend/Controllers/WebhooksController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using VersopayBackend.Dtos;
using VersopayBackend.Dtos.VexyBank;
using VersopayBackend.Repositories;
using VersopayBackend.Services;
using VersopayBackend.Services.Vexy;
using VersopayBackend.Utils;
using VersopayLibrary.Enums;

namespace VersopayBackend.Controllers
{
    [ApiController]
    [Route("api/webhooks")]
    public class WebhooksController : ControllerBase
    {
        private readonly IWebhooksService _webhooksService;
        private readonly IInboundWebhookService _inboundWebhookService;
        private readonly IProviderCredentialRepository _providerCredRepo;
        private readonly ILogger<WebhooksController> _logger;

        public WebhooksController(
            IWebhooksService webhooksService,
            IInboundWebhookService inboundWebhookService,
            IProviderCredentialRepository providerCredRepo,
            ILogger<WebhooksController> logger)
        {
            _webhooksService = webhooksService;
            _inboundWebhookService = inboundWebhookService;
            _providerCredRepo = providerCredRepo;
            _logger = logger;
        }

        // ============================
        // OUTBOUND CRUD (exige JWT)
        // ============================

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<WebhookResponseDto>> Create([FromBody] WebhookCreateDto body, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var res = await _webhooksService.CreateAsync(body, ct);
            return CreatedAtAction(nameof(GetById), new { id = res.Id }, res);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<WebhookResponseDto>>> GetAll([FromQuery] bool? ativo, CancellationToken ct)
        {
            var res = await _webhooksService.GetAllAsync(ativo, ct);
            return Ok(res);
        }

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<ActionResult<WebhookResponseDto>> GetById(int id, CancellationToken ct)
        {
            var res = await _webhooksService.GetByIdAsync(id, ct);
            return res is null ? NotFound() : Ok(res);
        }

        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<ActionResult<WebhookResponseDto>> Update(int id, [FromBody] WebhookUpdateDto body, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var res = await _webhooksService.UpdateAsync(id, body, ct);
            return res is null ? NotFound() : Ok(res);
        }

        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var ok = await _webhooksService.DeleteAsync(id, ct);
            return ok ? NoContent() : NotFound();
        }

        [HttpPost("{id:int}/test")]
        [Authorize]
        public async Task<IActionResult> SendTest(int id, [FromBody] WebhookTestPayloadDto payload, CancellationToken ct)
        {
            var delivered = await _webhooksService.SendTestAsync(id, payload, ct);
            return delivered ? Ok(new { delivered = true }) : BadRequest(new { delivered = false });
        }

        // ==========================================
        // INBOUND (PROVIDERS) - público (webhooks)
        // ==========================================

        /// <summary>Versell Pay</summary>
        [HttpPost("providers/versell")]
        [AllowAnonymous]
        [Consumes("application/json")]
        public async Task<IActionResult> InboundVersell([FromBody] VersellWebhookDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            var status = await _inboundWebhookService.HandleVersellAsync(dto, ip, headers, ct);

            return status switch
            {
                ProcessingStatus.Success => Ok(new { ok = true }),
                ProcessingStatus.Duplicate => Ok(new { ok = true, duplicate = true }),
                ProcessingStatus.InvalidAuth => Unauthorized(),
                _ => StatusCode(500, new { ok = false })
            };
        }

        /// <summary>VexyPayments – rota antiga /providers/vexy</summary>
        [HttpPost("providers/vexy")]
        [AllowAnonymous]
        [Consumes("application/json")]
        public async Task<IActionResult> InboundVexy(CancellationToken ct)
        {
            // 1) raw body (necessário p/ HMAC)
            HttpContext.Request.EnableBuffering();
            string rawBody;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
            {
                rawBody = await reader.ReadToEndAsync();
                Request.Body.Position = 0;
            }

            // 2) headers/ip
            var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

            // 3) tentar HMAC somente se houver secrets no banco
            var allSecrets = await _providerCredRepo.GetAllVexyWebhookSecretsAsync(ct);
            var mustRequireHmac = allSecrets.Any(s => !string.IsNullOrWhiteSpace(s));

            bool signatureOk = !mustRequireHmac; // em DEV, se não há secrets, não exige HMAC
            if (mustRequireHmac)
            {
                var signatureHeader =
                    Request.Headers["Vexy-Signature"].FirstOrDefault()
                    ?? Request.Headers["vexy-signature"].FirstOrDefault();

                foreach (var secret in allSecrets)
                {
                    if (string.IsNullOrWhiteSpace(secret)) continue;
                    if (VexySignatureVerifier.Verify(signatureHeader, rawBody, secret, timestampToleranceSeconds: 300))
                    {
                        signatureOk = true;
                        break;
                    }
                }
                if (!signatureOk)
                {
                    _logger.LogWarning("Assinatura Vexy-Signature ausente/invalidada e há secrets cadastrados. Rejeitando.");
                    return Unauthorized(); // em PROD: rejeita quando há secret e assinatura falhou
                }
            }

            // 4) desserializa DTO
            VexyWebhookDto? dto;
            try
            {
                dto = JsonSerializer.Deserialize<VexyWebhookDto>(
                    rawBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Payload Vexy inválido (providers).");
                return BadRequest("JSON inválido.");
            }
            if (dto is null) return BadRequest("Payload ausente.");

            // 5) processa via serviço (ele ainda pode verificar fallback de headers e retornar InvalidAuth)
            var status = await _inboundWebhookService.HandleVexyAsync(dto, ip, headers, ct);

            // 6) resposta
            return status switch
            {
                ProcessingStatus.Success => Ok(new { ok = true, verified = signatureOk }),
                ProcessingStatus.Duplicate => Ok(new { ok = true, duplicate = true, verified = signatureOk }),
                ProcessingStatus.InvalidAuth =>
                    // Em DEV você pode optar por 200 para inspecionar; em PROD mantenha 401:
                    Unauthorized(),
                _ => StatusCode(500, new { ok = false })
            };
        }


        /// <summary>VexyBank – nova rota por owner (pix-in | pix-out)</summary>
        /// <remarks>
        /// POST /api/webhooks/v1/vexy/{ownerUserId}/{channel}
        /// Header esperado (exemplo): Vexy-Signature: t=1730820000,v1=<hex_hmac>
        /// A assinatura é HMAC-SHA256 sobre a string $"{t}.{rawBody}" usando o segredo cadastrado.
        /// </remarks>
        [HttpPost("v1/vexy/{ownerUserId:int}/{channel}")]
        [AllowAnonymous]
        [Consumes("application/json")]
        public async Task<IActionResult> InboundVexyV1(
            int ownerUserId,
            string channel,
            [FromServices] IVexyBankService vexyService,
            CancellationToken ct)
        {
            // 1) Captura corpo cru (obrigatório para HMAC sobre o payload exato)
            HttpContext.Request.EnableBuffering();
            string rawBody;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
            {
                rawBody = await reader.ReadToEndAsync();
                Request.Body.Position = 0;
            }

            // 2) Headers e IP de origem (para logs e auditoria)
            var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
            var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            // 3) Verificação HMAC (Modelo A: segredo atual + anterior)
            bool verified = true; // DEV-permissivo quando não há segredo no BD
            try
            {
                var signatureHeader =
                    Request.Headers["Vexy-Signature"].FirstOrDefault()
                    ?? Request.Headers["vexy-signature"].FirstOrDefault();

                // Busca par de segredos para este owner (Atual + Anterior)
                var (current, previous) = await _providerCredRepo.GetVexySecretsPairAsync(ownerUserId, ct);
                var candidates = new[] { current, previous }
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToArray();

                if (candidates.Length > 0)
                {
                    // Se há segredo cadastrado, a verificação passa a ser OBRIGATÓRIA
                    verified = false;
                    foreach (var secret in candidates)
                    {
                        if (VexySignatureVerifier.Verify(signatureHeader, rawBody, secret!, timestampToleranceSeconds: 300))
                        {
                            verified = true;
                            break;
                        }
                    }

                    if (!verified)
                    {
                        _logger.LogWarning(
                            "InboundVexyV1: assinatura HMAC inválida (Owner={Owner}, Channel={Channel}). Header={Header}",
                            ownerUserId, channel, signatureHeader ?? "<null>");

                        // Em produção: rejeita. Em DEV, pode retornar 401 para testar a assinatura.
                        return Unauthorized(new { ok = false, reason = "invalid_signature" });
                    }
                }
            }
            catch (Exception ex)
            {
                // Em produção, prefira rejeitar se a validação falhar por exceção.
                _logger.LogWarning(ex, "InboundVexyV1: falha ao validar assinatura HMAC (Owner={Owner})", ownerUserId);
                // return Unauthorized(new { ok = false, reason = "signature_validation_error" });
                // Mantemos permissivo apenas se você quiser testar sem segredo no BD.
            }

            // 4) Desserializa o ENVELOPE do webhook
            VexyWebhookEnvelope? payload;
            try
            {
                payload = JsonSerializer.Deserialize<VexyWebhookEnvelope>(
                    rawBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "InboundVexyV1: JSON inválido (Owner={Owner}). Body='{Body}'", ownerUserId, rawBody);
                return BadRequest("JSON inválido.");
            }
            if (payload is null)
                return BadRequest("Payload ausente.");

            // (Opcional) Sanidade simples por canal, se desejar:
            // if (channel.Equals("pix-in", StringComparison.OrdinalIgnoreCase) && !string.Equals(payload.Type, "transaction", StringComparison.OrdinalIgnoreCase)) { ... }
            // if (channel.Equals("pix-out", StringComparison.OrdinalIgnoreCase) && !string.Equals(payload.Type, "transfer", StringComparison.OrdinalIgnoreCase)) { ... }

            // 5) Processamento de domínio / persistência / idempotência
            await vexyService.HandleWebhookAsync(ownerUserId, payload, sourceIp, headers, ct);

            // 6) Retorno
            return Ok(new { ok = true, verified });
        }
    }
}
