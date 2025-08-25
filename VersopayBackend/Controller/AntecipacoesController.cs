using Microsoft.AspNetCore.Mvc;
using VersopayBackend.Dtos;
using VersopayBackend.Services;

namespace VersopayBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AntecipacoesController(IAntecipacoesService svc) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<AntecipacaoResponseDto>> Create([FromBody] AntecipacaoCreateDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try
            {
                var res = await svc.CreateAsync(dto, ct);
                return CreatedAtAction(nameof(GetById), new { id = res.Id }, res);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AntecipacaoResponseDto>>> GetAll(
            [FromQuery] int? empresaId,
            [FromQuery] string? status,
            [FromQuery] DateTime? deUtc,
            [FromQuery] DateTime? ateUtc,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var res = await svc.GetAllAsync(empresaId, status, deUtc, ateUtc, page, pageSize, ct);
            return Ok(res);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<AntecipacaoResponseDto>> GetById(int id, CancellationToken ct)
        {
            var res = await svc.GetByIdAsync(id, ct);
            return res is null ? NotFound() : Ok(res);
        }

        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] AntecipacaoStatusUpdateDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try
            {
                var ok = await svc.UpdateStatusAsync(id, dto, ct);
                return ok ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        }
    }
}
