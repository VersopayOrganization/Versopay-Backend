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
        public async Task<ActionResult<KycKybResponseDto>> Create([FromBody] KycKybCreateDto dto, CancellationToken ct)
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
        public async Task<ActionResult<IEnumerable<KycKybResponseDto>>> GetAll(
            [FromQuery] int? usuarioId,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var res = await svc.GetAllAsync(usuarioId, status, page, pageSize, ct);
            return Ok(res);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<KycKybResponseDto>> GetById(int id, CancellationToken ct)
        {
            var res = await svc.GetByIdAsync(id, ct);
            return res is null ? NotFound() : Ok(res);
        }

        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] KycKybStatusUpdateDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var ok = await svc.UpdateStatusAsync(id, dto, ct);
            return ok ? NoContent() : NotFound();
        }

        [HttpPost("{id:int}/aprovar")]
        public async Task<IActionResult> Aprovar(int id, CancellationToken ct)
        {
            var ok = await svc.AprovarAsync(id, ct);
            return ok ? NoContent() : NotFound();
        }

        [HttpPost("{id:int}/reprovar")]
        public async Task<IActionResult> Reprovar(int id, CancellationToken ct)
        {
            var ok = await svc.ReprovarAsync(id, ct);
            return ok ? NoContent() : NotFound();
        }
    }
}
