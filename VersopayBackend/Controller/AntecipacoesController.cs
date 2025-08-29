using Microsoft.AspNetCore.Mvc;
using VersopayBackend.Dtos;
using VersopayBackend.Services;

namespace VersopayBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AntecipacoesController(IAntecipacoesService iAntecipacoesService) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<AntecipacaoResponseDto>> Create([FromBody] AntecipacaoCreateDto antecipacaoCreateDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try
            {
                var res = await iAntecipacoesService.CreateAsync(antecipacaoCreateDto, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = res.Id }, res);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AntecipacaoResponseDto>>> GetAll(
            [FromQuery] int? empresaId,
            [FromQuery] string? status,
            [FromQuery] DateTime? dataInicio,
            [FromQuery] DateTime? dataFim,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var res = await iAntecipacoesService.GetAllAsync(empresaId, status, dataInicio, dataFim, page, pageSize, cancellationToken);
            return Ok(res);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<AntecipacaoResponseDto>> GetById(int id, CancellationToken cancellationToken)
        {
            var res = await iAntecipacoesService.GetByIdAsync(id, cancellationToken);
            return res is null ? NotFound() : Ok(res);
        }

        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] AntecipacaoStatusUpdateDto antecipacaoStatusUpdateDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try
            {
                var ok = await iAntecipacoesService.UpdateStatusAsync(id, antecipacaoStatusUpdateDto, cancellationToken);
                return ok ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        }
    }
}
