using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VersopayBackend.Dtos;
using VersopayBackend.Services;

namespace VersopayBackend.Controllers
{
    [ApiController]
    [Route("api/extrato")]
    [Authorize]
    public class ExtratoController : ControllerBase
    {
        private readonly IExtratoService _service;
        public ExtratoController(IExtratoService service) => _service = service;

        [HttpGet("{clienteId:int}")]
        public async Task<ActionResult<ExtratoResponseDto>> GetByCliente(int clienteId, CancellationToken cancellationToken)
        {
            var dto = await _service.GetByClienteAsync(clienteId, cancellationToken);
            return Ok(dto);
        }

        [HttpPost("movimentacoes")]
        public async Task<ActionResult<MovimentacaoResponseDto>> Lancar([FromBody] MovimentacaoCreateDto body, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try
            {
                var dto = await _service.LancarAsync(body, cancellationToken);
                return CreatedAtAction(nameof(GetByCliente), new { clienteId = body.ClienteId }, dto);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("movimentacoes/{id:guid}/confirmar")]
        public async Task<ActionResult<MovimentacaoResponseDto>> Confirmar(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var dto = await _service.ConfirmarAsync(id, cancellationToken);
                return dto is null ? NotFound() : Ok(dto);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("movimentacoes/{id:guid}/cancelar")]
        public async Task<IActionResult> Cancelar(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var ok = await _service.CancelarAsync(id, cancellationToken);
                return ok ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("{clienteId:int}/movimentacoes")]
        public async Task<ActionResult<IEnumerable<MovimentacaoResponseDto>>> Listar(
            int clienteId, [FromQuery] MovimentacaoFiltroDto filtro, CancellationToken cancellationToken)
        {
            var list = await _service.ListarMovimentacoesAsync(clienteId, filtro, cancellationToken);
            return Ok(list);
        }
    }
}
