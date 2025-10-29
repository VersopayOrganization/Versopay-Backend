using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VersopayBackend.Dtos;
using VersopayBackend.Services;
using VersopayLibrary.Enums;

namespace VersopayBackend.Controllers // <- padronize "Controllers", não "Controller"
{
    [ApiController]
    [Route("api/webhooks")]
    [Authorize]
    public class WebhooksController(
       IWebhooksService webhooksService,
       IInboundWebhookService inboundWebhookService, // totalmente qualificado (garante a Opção 1)
       ILogger<WebhooksController> logger
   ) : ControllerBase
    {
        // ==========================
        // OUTBOUND (o que você já tinha)
        // ==========================
        [HttpPost]
        public async Task<ActionResult<WebhookResponseDto>> Create([FromBody] WebhookCreateDto webhookCreateDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var response = await webhooksService.CreateAsync(webhookCreateDto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<WebhookResponseDto>>> GetAll([FromQuery] bool? ativo, CancellationToken cancellationToken)
        {
            var response = await webhooksService.GetAllAsync(ativo, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<WebhookResponseDto>> GetById(int id, CancellationToken cancellationToken)
        {
            var response = await webhooksService.GetByIdAsync(id, cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<WebhookResponseDto>> Update(int id, [FromBody] WebhookUpdateDto webhookUpdateDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var response = await webhooksService.UpdateAsync(id, webhookUpdateDto, cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var ok = await webhooksService.DeleteAsync(id, cancellationToken);
            return ok ? NoContent() : NotFound();
        }

        [HttpPost("{id:int}/test")]
        public async Task<IActionResult> SendTest(int id, [FromBody] WebhookTestPayloadDto payload, CancellationToken cancellationToken)
        {
            var delivered = await webhooksService.SendTestAsync(id, payload, cancellationToken);
            return delivered ? Ok(new { delivered = true }) : BadRequest(new { delivered = false });
        }

        // ==========================
        // INBOUND (novos endpoints para provedores)
        // ==========================

        // Versell Pay envia depósitos/chargeback
        // POST /api/webhooks/providers/versell
        [HttpPost("providers/versell")]
        [AllowAnonymous]
        [Consumes("application/json")]
        public async Task<IActionResult> InboundVersell([FromBody] VersellWebhookDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            var status = await inboundWebhookService.HandleVersellAsync(dto, ip, headers, ct);

            return status switch
            {
                ProcessingStatus.Success => Ok(new { ok = true }),
                ProcessingStatus.Duplicate => Ok(new { ok = true, duplicate = true }),
                ProcessingStatus.InvalidAuth => Unauthorized(),
                _ => StatusCode(500, new { ok = false })
            };
        }

        // VexyPayments envia depósitos/saques/RETIDO (MED)
        // POST /api/webhooks/providers/vexy
        [HttpPost("providers/vexy")]
        [AllowAnonymous]
        [Consumes("application/json")]
        public async Task<IActionResult> InboundVexy([FromBody] VexyWebhookDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            var status = await inboundWebhookService.HandleVexyAsync(dto, ip, headers, ct);

            return status switch
            {
                ProcessingStatus.Success => Ok(new { ok = true }),
                ProcessingStatus.Duplicate => Ok(new { ok = true, duplicate = true }),
                ProcessingStatus.InvalidAuth => Unauthorized(),
                _ => StatusCode(500, new { ok = false })
            };
        }
    }
}
