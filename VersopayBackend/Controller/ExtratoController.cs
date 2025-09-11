using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VersopayBackend.Dtos;
using VersopayBackend.Services;

namespace VersopayBackend.Controller
{
    [ApiController]
    [Route("api/extrato")]
    [Authorize] // exige JWT
    public class ExtratoController(IExtratoService service) : ControllerBase
    {
        [HttpGet("{clienteId:int}")]
        public async Task<ActionResult<ExtratoResponseDto>> GetByCliente(int clienteId, CancellationToken cancellationToken)
        {
            var extratoResponseDto = await service.GetByClienteAsync(clienteId, cancellationToken);
            return Ok(extratoResponseDto);
        }

        [HttpPost("movimentacoes")]
        public async Task<ActionResult<MovimentacaoResponseDto>> Lancar([FromBody] MovimentacaoCreateDto movimentacaoCreateDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            try
            {
                var movimentacaoResponseDto = await service.LancarAsync(movimentacaoCreateDto, cancellationToken);
                return CreatedAtAction(nameof(GetByCliente), new { clienteId = movimentacaoCreateDto.ClienteId }, movimentacaoResponseDto);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("movimentacoes/{id:guid}/confirmar")]
        public async Task<ActionResult<MovimentacaoResponseDto>> Confirmar(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var movimentacaoResponseDto = await service.ConfirmarAsync(id, cancellationToken);
                return movimentacaoResponseDto is null ? NotFound() : Ok(movimentacaoResponseDto);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("movimentacoes/{id:guid}/cancelar")]
        public async Task<IActionResult> Cancelar(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var ok = await service.CancelarAsync(id, cancellationToken);
                return ok ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{clienteId:int}/movimentacoes")]
        public async Task<ActionResult<IEnumerable<MovimentacaoResponseDto>>> Listar(
            int clienteId, [FromQuery] MovimentacaoFiltroDto movimentacaoFiltroDto, CancellationToken cancellationToken)
        {
            var movimentacaoResponseDto = await service.ListarMovimentacoesAsync(clienteId, movimentacaoFiltroDto, cancellationToken);
            return Ok(movimentacaoResponseDto);
        }
    }
}
