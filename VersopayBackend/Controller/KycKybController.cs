using Microsoft.AspNetCore.Mvc;
using VersopayBackend.Dtos;
using VersopayBackend.Services;
using VersopayBackend.Services.KycKyb;

namespace VersopayBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize(Roles = "Admin")] // habilite se apenas admins puderem aprovar/reprovar
    public class KycKybController(IKycKybService svc) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<KycKybResponseDto>> Criar([FromBody] KycKybCreateDto kycKybCreateDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try
            {
                var res = await svc.CriarAsync(kycKybCreateDto, cancellationToken);
                return CreatedAtAction(nameof(PegarPeloId), new { id = res.Id }, res);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<KycKybResponseDto>>> PegarTodos(
            [FromQuery] int? usuarioId,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var res = await svc.PegarTodosAsync(usuarioId, status, page, pageSize, cancellationToken);
            return Ok(res);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<KycKybResponseDto>> PegarPeloId(int id, CancellationToken cancellationToken)
        {
            var res = await svc.PegarPeloIdAsync(id, cancellationToken);
            return res is null ? NotFound() : Ok(res);
        }

        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> AtualizarStatus(int id, [FromBody] KycKybStatusUpdateDto kycKybCreateDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var ok = await svc.AtualizarStatusAsync(id, kycKybCreateDto, cancellationToken);
            return ok ? NoContent() : NotFound();
        }

        [HttpPost("{id:int}/aprovar")]
        public async Task<IActionResult> Aprovar(int id, CancellationToken cancellationToken)
        {
            var ok = await svc.AprovarAsync(id, cancellationToken);
            return ok ? NoContent() : NotFound();
        }

        [HttpPost("{id:int}/reprovar")]
        public async Task<IActionResult> Reprovar(int id, CancellationToken cancellationToken)
        {
            var ok = await svc.ReprovarAsync(id, cancellationToken);
            return ok ? NoContent() : NotFound();
        }
    }
}
