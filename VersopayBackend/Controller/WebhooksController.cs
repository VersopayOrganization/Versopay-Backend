// VersopayBackend/Controllers/WebhooksController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using VersopayBackend.Dtos;
using VersopayBackend.Dtos.Common;
using VersopayBackend.Dtos.VexyBank;
using VersopayBackend.Repositories;
using VersopayBackend.Repositories.VexyClient.PixIn;
using VersopayBackend.Services;
using VersopayBackend.Services.Vexy;
using VersopayBackend.Utils;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

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
        private readonly IInboundWebhookLogRepository _inboundLogRepo;
        private readonly IVexyBankPixInRepository _pixInRepo;
        private readonly IPedidoRepository _pedidoRepo;
        private readonly IPedidoMatchRepository _pedidoMatchRepo;
        private readonly ITransferenciaMatchRepository _transferMatchRepo;
        private readonly ITransferenciaRepository _transferRepo;
        private readonly IVendorWebhookHandlerFactory _handlerFactory;
        private readonly IPedidosService _pedidosService;
        private readonly ITransferenciasService _transferenciasService;

        public WebhooksController(
        ILogger<WebhooksController> logger,
        IInboundWebhookLogRepository inboundLogRepo,
        IVexyBankPixInRepository pixInRepo,
        IPedidoRepository pedidoRepo,
        IPedidoMatchRepository pedidoMatchRepo,
        ITransferenciaMatchRepository transferMatchRepo,
        ITransferenciaRepository transferRepo,
        IVendorWebhookHandlerFactory handlerFactory,
        IPedidosService pedidosService,                 // <= add
        ITransferenciasService transferenciasService)    // <= add (opcional, para pix-out))
        {
            _logger = logger;
            _inboundLogRepo = inboundLogRepo;
            _pixInRepo = pixInRepo;
            _pedidoRepo = pedidoRepo;
            _pedidoMatchRepo = pedidoMatchRepo;
            _transferMatchRepo = transferMatchRepo;
            _transferRepo = transferRepo;
            _handlerFactory = handlerFactory;
            _pedidosService = pedidosService;               // <= add
            _transferenciasService = transferenciasService; // <= add
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


        /// <summary>VexyBank – webhook por owner (pix-in | pix-out)</summary>
        /// Ex.: POST /api/webhooks/v1/vexy/{ownerUserId}/{channel}
        /// channel: "pix-in" ou "pix-out"
        [HttpPost("v1/{provider}/{ownerUserId:int}/{channel}")]
        [AllowAnonymous]
        [Consumes("application/json")]
        public async Task<IActionResult> InboundGeneric(
            [FromRoute] string provider,
            [FromRoute] int ownerUserId,
            [FromRoute] string channel,
            CancellationToken ct)
        {
            // 1) raw body + headers/ip
            HttpContext.Request.EnableBuffering();
            string rawBody;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
            {
                rawBody = await reader.ReadToEndAsync();
                Request.Body.Position = 0;
            }
            var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
            var ip = HttpContext.Connection?.RemoteIpAddress?.ToString() ?? "";

            // 2) resolve handler pelo provider
            var handler = _handlerFactory.Resolve(provider);

            // 3) verifica assinatura (se aplicável para o handler)
            var okSig = await handler.VerifySignatureAsync(ownerUserId, rawBody, headers, ct);
            if (!okSig) return Unauthorized();

            // 4) dedupe por event key
            var eventKey = handler.BuildEventKey(rawBody, headers).ToLowerInvariant();
            if (await _inboundLogRepo.ExistsByEventKeyAsync(eventKey, ct))
                return Ok(new { ok = true, duplicated = true });

            // 5) processa (handler cria/atualiza Pedido/Transferência e devolve o log pronto)
            var log = await handler.ProcessAsync(ownerUserId, channel, rawBody, headers, ip, ct);

            await _inboundLogRepo.AddAsync(log, ct);
            await _inboundLogRepo.SaveChangesAsync(ct);

            return Ok(new { ok = true });
        }
    }
}
