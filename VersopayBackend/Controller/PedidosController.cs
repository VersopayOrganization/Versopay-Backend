using Microsoft.AspNetCore.Mvc;
// using Microsoft.AspNetCore.Authorization;
using VersopayBackend.Dtos;
using VersopayBackend.Services;

namespace VersopayBackend.Controllers
{
    [ApiController]
    [Route("api/pedidos")]
    // [Authorize]
    public class PedidosController : ControllerBase
    {
        private readonly IPedidosService _svc;
        public PedidosController(IPedidosService svc) => _svc = svc;

        [HttpPost]
        public async Task<ActionResult<PedidoDto>> Create([FromBody] PedidoCreateDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try
            {
                var res = await _svc.CreateAsync(dto, ct);
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
            var list = await _svc.GetAllAsync(status, vendedorId, metodo, dataDe, dataAte, page, pageSize, ct);
            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<PedidoDto>> GetById(int id, CancellationToken ct)
        {
            var res = await _svc.GetByIdAsync(id, ct);
            return res is null ? NotFound() : Ok(res);
        }

        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] PedidoStatusUpdateDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var ok = await _svc.UpdateStatusAsync(id, dto, ct);
            return ok ? NoContent() : NotFound();
        }

        [HttpPost("{id:int}/pagar")]
        public async Task<IActionResult> MarcarComoPago(int id, CancellationToken ct)
        {
            var ok = await _svc.MarcarComoPagoAsync(id, ct);
            return ok ? NoContent() : NotFound();
        }
    }
}
