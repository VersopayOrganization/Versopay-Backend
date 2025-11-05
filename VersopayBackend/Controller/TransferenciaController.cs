using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VersopayBackend.Dtos;
using VersopayBackend.Services;
using VersopayLibrary.Enums;

namespace VersopayBackend.Controllers
{
    [ApiController]
    [Route("api/transferencias")]
    public class TransferenciaController : ControllerBase
    {
        private readonly ITransferenciasService _transferenciaService;
        public TransferenciaController(ITransferenciasService transferenciaService) => _transferenciaService = transferenciaService;

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<TransferenciaResponseDto>>> GetAll(
            [FromQuery] int? solicitanteId,
            [FromQuery] StatusTransferencia? status,
            [FromQuery] DateTime? dataInicio,
            [FromQuery] DateTime? dataFim,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var list = await _transferenciaService.GetAllAsync(solicitanteId, status, dataInicio, dataFim, page, pageSize, cancellationToken);
            return Ok(list);
        }

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<ActionResult<TransferenciaResponseDto>> GetById(int id, CancellationToken cancellationToken)
        {
            var dto = await _transferenciaService.GetByIdAsync(id, cancellationToken);
            return dto is null ? NotFound() : Ok(dto);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<TransferenciaResponseDto>> Create([FromBody] TransferenciaCreateDto body, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try
            {
                var dto = await _transferenciaService.CreateAsync(body, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPut("{id:int}")]
        [Authorize] // role Admin?
        public async Task<ActionResult<TransferenciaResponseDto>> AdminUpdate(int id, [FromBody] TransferenciaAdminUpdateDto body, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var dto = await _transferenciaService.AdminUpdateAsync(id, body, cancellationToken);
            return dto is null ? NotFound() : Ok(dto);
        }

        [HttpPost("{id:int}/cancelar")]
        [Authorize] // role Admin?
        public async Task<IActionResult> Cancelar(int id, CancellationToken cancellationToken)
        {
            var ok = await _transferenciaService.CancelarAsync(id, cancellationToken);
            return ok ? NoContent() : NotFound();
        }

        [HttpPost("{id:int}/concluir")]
        [Authorize] // role Admin?
        public async Task<IActionResult> Concluir(int id, [FromBody] ConcluirBody body, CancellationToken cancellationToken)
        {
            var ok = await _transferenciaService.ConcluirAsync(id, body.Taxa, body.ValorFinal, cancellationToken);
            return ok ? NoContent() : NotFound();
        }

        public sealed class ConcluirBody
        {
            public decimal? Taxa { get; set; }
            public decimal? ValorFinal { get; set; }
        }
    }
}
