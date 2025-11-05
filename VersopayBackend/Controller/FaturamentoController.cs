using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VersopayBackend.Dtos;
using VersopayBackend.Services;

namespace VersopayBackend.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/faturamento")]
    public class FaturamentoController : ControllerBase
    {
        private readonly IFaturamentoService _service;
        public FaturamentoController(IFaturamentoService service) => _service = service;

        [HttpGet("{id:int}")]
        public async Task<ActionResult<FaturamentoDto>> GetById(int id, CancellationToken ct)
        {
            var dto = await _service.GetByIdAsync(id, ct);
            return dto is null ? NotFound() : Ok(dto);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FaturamentoDto>>> Listar(
            [FromQuery] string cpfCnpj,
            [FromQuery] DateTime? inicioUtc,
            [FromQuery] DateTime? fimUtc,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(cpfCnpj))
                return BadRequest(new { message = "Informe cpfCnpj." });

            var list = await _service.ListarAsync(cpfCnpj, inicioUtc, fimUtc, ct);
            return Ok(list);
        }

        [HttpPost("recalcular")]
        public async Task<ActionResult<FaturamentoDto>> Recalcular(
            [FromBody] FaturamentoRecalcularRequest body,
            CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            try
            {
                var dto = await _service.RecalcularAsync(body, ct);
                return body.Salvar
                    ? CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto)
                    : Ok(dto);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }
    }
}
