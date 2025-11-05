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
    [Authorize]
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

        // =========================================================
        // OUTBOUND (CRUD)
        // =========================================================

        [HttpPost]
        public async Task<ActionResult<WebhookResponseDto>> Create(
            [FromBody] WebhookCreateDto webhookCreateDto,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var response = await _webhooksService.CreateAsync(webhookCreateDto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<WebhookResponseDto>>> GetAll(
            [FromQuery] bool? ativo,
            CancellationToken cancellationToken)
        {
            var response = await _webhooksService.GetAllAsync(ativo, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<WebhookResponseDto>> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var response = await _webhooksService.GetByIdAsync(id, cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<WebhookResponseDto>> Update(
            int id,
            [FromBody] WebhookUpdateDto webhookUpdateDto,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var response = await _webhooksService.UpdateAsync(id, webhookUpdateDto, cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(
            int id,
            CancellationToken cancellationToken)
        {
            var ok = await _webhooksService.DeleteAsync(id, cancellationToken);
            return ok ? NoContent() : NotFound();
        }

        [HttpPost("{id:int}/test")]
        public async Task<IActionResult> SendTest(
            int id,
            [FromBody] WebhookTestPayloadDto payload,
            CancellationToken cancellationToken)
        {
            var delivered = await _webhooksService.SendTestAsync(id, payload, cancellationToken);
            return delivered ? Ok(new { delivered = true }) : BadRequest(new { delivered = false });
        }

        // =========================================================
        // INBOUND (provedores)
        // =========================================================

        /// <summary>
        /// Versell Pay – depósitos/chargeback
        /// POST /api/webhooks/providers/versell
        /// </summary>
        [HttpPost("providers/versell")]
        [AllowAnonymous]
        [Consumes("application/json")]
        public async Task<IActionResult> InboundVersell(
            [FromBody] VersellWebhookDto dto,
            CancellationToken ct)
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

        /// <summary>
        /// VexyPayments – transações PIX IN e transferências PIX OUT
        /// POST /api/webhooks/providers/vexy
        /// Exige verificação de assinatura (Vexy-Signature) com HMAC-SHA256.
        /// </summary>
        [HttpPost("providers/vexy")]
        [AllowAnonymous]
        [Consumes("application/json")]
        public async Task<IActionResult> InboundVexy(CancellationToken ct)
        {
            // 1) Capturar raw body (obrigatório para HMAC sobre payload cru)
            HttpContext.Request.EnableBuffering();
            string rawBody;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
            {
                rawBody = await reader.ReadToEndAsync();
                Request.Body.Position = 0;
            }

            // 2) Header de assinatura (case-insensitive)
            var signatureHeader =
                Request.Headers["Vexy-Signature"].FirstOrDefault()
                ?? Request.Headers["vexy-signature"].FirstOrDefault();

            // 3) Tentar validar assinatura contra todos os secrets cadastrados da Vexy
            bool signatureOk = false;
            try
            {
                var secrets = await _providerCredRepo.GetAllVexyWebhookSecretsAsync(ct);
                foreach (var secret in secrets)
                {
                    if (string.IsNullOrWhiteSpace(secret)) continue;
                    if (VexySignatureVerifier.Verify(signatureHeader, rawBody, secret, timestampToleranceSeconds: 300))
                    {
                        signatureOk = true;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao obter secrets para verificação de assinatura Vexy.");
            }

            // 4) Desserializar DTO mantendo o payload cru para logs idempotentes
            VexyWebhookDto? dto;
            try
            {
                dto = System.Text.Json.JsonSerializer.Deserialize<VexyWebhookDto>(
                    rawBody,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Payload Vexy inválido.");
                return BadRequest("JSON inválido.");
            }

            if (dto is null) return BadRequest("Payload ausente.");

            var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

            // 5) Se a assinatura bateu, processa. Caso contrário, você pode:
            //    - Rejeitar (recomendado em produção) OU
            //    - Cair em fallback de auth por headers (se você ainda mantiver essa compat)
            if (!signatureOk)
            {
                _logger.LogWarning("Assinatura Vexy-Signature inválida ou ausente.");
                // Em produção, prefira retornar Unauthorized():
                // return Unauthorized();

                // Fallback: processar mesmo assim (compat), deixando rastreado que não foi verificado
                var statusFallback = await _inboundWebhookService.HandleVexyAsync(dto, ip, headers, ct);
                return statusFallback switch
                {
                    ProcessingStatus.Success => Ok(new { ok = true, verified = false }),
                    ProcessingStatus.Duplicate => Ok(new { ok = true, duplicate = true, verified = false }),
                    ProcessingStatus.InvalidAuth => Unauthorized(),
                    _ => StatusCode(500, new { ok = false })
                };
            }

            // 6) Assinatura válida – processa normalmente
            var status = await _inboundWebhookService.HandleVexyAsync(dto, ip, headers, ct);

            return status switch
            {
                ProcessingStatus.Success => Ok(new { ok = true }),
                ProcessingStatus.Duplicate => Ok(new { ok = true, duplicate = true }),
                ProcessingStatus.InvalidAuth => Unauthorized(),
                _ => StatusCode(500, new { ok = false })
            };
        }

        /// <summary>
        /// VexyBank Webhook (API nova) – PIX IN / PIX OUT
        /// POST /api/webhooks/v1/vexy/{ownerUserId}/{channel}
        ///   channel: "pix-in" | "pix-out"
        /// Verificação por HMAC no header "Vexy-Signature" (opcional, mas recomendado).
        /// </summary>
        [HttpPost("v1/vexy/{ownerUserId:int}/{channel}")]
        [AllowAnonymous]
        [Consumes("application/json")]
        public async Task<IActionResult> InboundVexyV1(
            int ownerUserId,
            string channel,
            [FromServices] IVexyBankService vexyService,
            CancellationToken ct)
        {
            // 1) raw body
            HttpContext.Request.EnableBuffering();
            string rawBody;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
            {
                rawBody = await reader.ReadToEndAsync();
                Request.Body.Position = 0;
            }

            // 2) headers e ip
            var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
            var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString();

            // 3) Verificação da assinatura (se houver secret cadastrado)
            bool signatureOk = true; // default "permissivo" para ambientes de dev
            try
            {
                var signatureHeader =
                    Request.Headers["Vexy-Signature"].FirstOrDefault()
                    ?? Request.Headers["vexy-signature"].FirstOrDefault();

                var secrets = await _providerCredRepo.GetVexyWebhookSecretsByOwnerAsync(ownerUserId, ct);
                if (secrets is not null && secrets.Any(s => !string.IsNullOrWhiteSpace(s)))
                {
                    signatureOk = false;
                    foreach (var secret in secrets)
                    {
                        if (string.IsNullOrWhiteSpace(secret)) continue;
                        if (VexySignatureVerifier.Verify(signatureHeader, rawBody, secret, timestampToleranceSeconds: 300))
                        {
                            signatureOk = true;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao validar assinatura da Vexy para Owner={Owner}", ownerUserId);
                // mantenha permissivo em DEV; em PROD pode retornar 401.
            }

            // 4) Desserializa envelope novo
            VexyWebhookEnvelope? payload;
            try
            {
                payload = JsonSerializer.Deserialize<VexyWebhookEnvelope>(
                    rawBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Payload Vexy inválido (novo formato).");
                return BadRequest("JSON inválido.");
            }

            if (payload is null) return BadRequest("Payload ausente.");

            // 5) (Opcional) Exigir assinatura válida em PROD:
            // if (!signatureOk) return Unauthorized();

            // 6) Delegar para o serviço especializado (já faz dedupe e logs)
            await vexyService.HandleWebhookAsync(ownerUserId, payload, sourceIp, headers, ct);

            return Ok(new { ok = true, verified = signatureOk });
        }
    }
}
