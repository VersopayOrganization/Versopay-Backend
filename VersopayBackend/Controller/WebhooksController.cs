using Microsoft.AspNetCore.Mvc;
using VersopayBackend.Dtos;
using VersopayBackend.Services.Webhooks;

namespace VersopayBackend.Controller
{
    [ApiController]
    [Route("api/webhooks")]
    public class WebhooksController(IWebhooksService IwebhookService) : ControllerBase
    {
        [HttpPost]
        //[Authorize(Roles = "Admin")]
        public async Task<ActionResult<WebhookResponseDto>> Create([FromBody] WebhookCreateDto webhookCreateDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var response = await IwebhookService.CreateAsync(webhookCreateDto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<WebhookResponseDto>>> GetAll([FromQuery] bool? ativo, CancellationToken cancellationToken)
        {
            var response = await IwebhookService.GetAllAsync(ativo, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<WebhookResponseDto>> GetById(int id, CancellationToken cancellationToken)
        {
            var response = await IwebhookService.GetByIdAsync(id, cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }

        [HttpPut("{id:int}")]
        //[Authorize(Roles = "Admin")]
        public async Task<ActionResult<WebhookResponseDto>> Update(int id, [FromBody] WebhookUpdateDto webhookUpdateDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var response = await IwebhookService.UpdateAsync(id, webhookUpdateDto, cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }

        [HttpDelete("{id:int}")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var response = await IwebhookService.DeleteAsync(id, cancellationToken);
            return response ? NoContent() : NotFound();
        }

        [HttpPost("{id:int}/test")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> SendTest(int id, [FromBody] WebhookTestPayloadDto webhookTestPayloadDto, CancellationToken cancellationToken)
        {
            var response = await IwebhookService.SendTestAsync(id, webhookTestPayloadDto, cancellationToken);
            return response ? Ok(new { delivered = true }) : BadRequest(new { delivered = false });
        }
    }
}
