using Microsoft.AspNetCore.Mvc;
using VersopayBackend.Dtos;
using VersopayBackend.Services;

namespace VersopayBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize] // habilite quando quiser
    public class PedidosController(IPedidosService svc) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<PedidoDto>> Create([FromBody] PedidoCreateDto dto, CancellationToken ct)
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
        public async Task<ActionResult<IEnumerable<PedidoDto>>> GetAll(
            [FromQuery] string? status,
            [FromQuery] int? vendedorId,
            [FromQuery] string? metodo,
            [FromQuery] DateTime? dataDe,
            [FromQuery] DateTime? dataAte,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var list = await svc.GetAllAsync(status, vendedorId, metodo, dataDe, dataAte, page, pageSize, ct);
            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<PedidoDto>> GetById(int id, CancellationToken ct)
        {
            var res = await svc.GetByIdAsync(id, ct);
            return res is null ? NotFound() : Ok(res);
        }

        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] PedidoStatusUpdateDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var ok = await svc.UpdateStatusAsync(id, dto, ct);
            return ok ? NoContent() : NotFound();
        }

        [HttpPost("{id:int}/pagar")]
        public async Task<IActionResult> MarcarComoPago(int id, CancellationToken ct)
        {
            var ok = await svc.MarcarComoPagoAsync(id, ct);
            return ok ? NoContent() : NotFound();
        }
    }
}
