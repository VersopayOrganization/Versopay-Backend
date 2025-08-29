using Microsoft.AspNetCore.Mvc;
using VersopayBackend.Dtos;
using VersopayBackend.Services.Webhooks;

namespace VersopayBackend.Controller
{
    [ApiController]
    [Route("api/webhooks")]
    public class WebhooksController(IWebhooksService svc) : ControllerBase
    {
        [HttpPost]
        //[Authorize(Roles = "Admin")]
        public async Task<ActionResult<WebhookResponseDto>> Create([FromBody] WebhookCreateDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var res = await svc.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = res.Id }, res);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<WebhookResponseDto>>> GetAll([FromQuery] bool? ativo, CancellationToken ct)
        {
            var res = await svc.GetAllAsync(ativo, ct);
            return Ok(res);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<WebhookResponseDto>> GetById(int id, CancellationToken ct)
        {
            var res = await svc.GetByIdAsync(id, ct);
            return res is null ? NotFound() : Ok(res);
        }

        [HttpPut("{id:int}")]
        //[Authorize(Roles = "Admin")]
        public async Task<ActionResult<WebhookResponseDto>> Update(int id, [FromBody] WebhookUpdateDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var res = await svc.UpdateAsync(id, dto, ct);
            return res is null ? NotFound() : Ok(res);
        }

        [HttpDelete("{id:int}")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var ok = await svc.DeleteAsync(id, ct);
            return ok ? NoContent() : NotFound();
        }

        [HttpPost("{id:int}/test")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> SendTest(int id, [FromBody] WebhookTestPayloadDto payload, CancellationToken ct)
        {
            var ok = await svc.SendTestAsync(id, payload, ct);
            return ok ? Ok(new { delivered = true }) : BadRequest(new { delivered = false });
        }
    }
}
