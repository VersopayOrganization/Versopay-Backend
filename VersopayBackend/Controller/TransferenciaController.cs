using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VersopayBackend.Dtos;
using VersopayBackend.Services;
using VersopayLibrary.Enums;

namespace VersopayBackend.Controller
{
    [ApiController]
    [Route("api/transferencias")]
    public class TransferenciaController(ITransferenciasService transferenciaService) : ControllerBase
    {
        // Lista com filtros opcionais
        [HttpGet]
        [Authorize] // ajuste conforme sua política
        public async Task<ActionResult<IEnumerable<TransferenciaResponseDto>>> GetAll(
            [FromQuery] int? solicitanteId,
            [FromQuery] StatusTransferencia? status,
            [FromQuery] DateTime? dataInicio,
            [FromQuery] DateTime? dataFim,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var transferenciaResponseList = await transferenciaService.GetAllAsync(solicitanteId, status, dataInicio, dataFim, page, pageSize, cancellationToken);
            return Ok(transferenciaResponseList);
        }

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<ActionResult<TransferenciaResponseDto>> GetById(int id, CancellationToken cancellationToken)
        {
            var transferenciaResponseDto = await transferenciaService.GetByIdAsync(id, cancellationToken);
            return transferenciaResponseDto is null ? NotFound() : Ok(transferenciaResponseDto);
        }

        [HttpPost]
        [Authorize] // ou AllowAnonymous se será solicitado pelo próprio usuário logado
        public async Task<ActionResult<TransferenciaResponseDto>> Create([FromBody] TransferenciaCreateDto transferenciaCreateDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try
            {
                var transferenciaResponseDto = await transferenciaService.CreateAsync(transferenciaCreateDto, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = transferenciaResponseDto.Id }, transferenciaResponseDto);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }

        // Atualização administrativa (status/aprovação/tipoEnvio/taxas etc)
        [HttpPut("{id:int}")]
        [Authorize] // role Admin?
        public async Task<ActionResult<TransferenciaResponseDto>> AdminUpdate(int id, [FromBody] TransferenciaAdminUpdateDto transferenciaAdminUpdateDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var transferenciaResponseDto = await transferenciaService.AdminUpdateAsync(id, transferenciaAdminUpdateDto, cancellationToken);
            return transferenciaResponseDto is null ? NotFound() : Ok(transferenciaResponseDto);
        }

        // atalhos opcionais
        [HttpPost("{id:int}/cancelar")]
        [Authorize] // role Admin?
        public async Task<IActionResult> Cancelar(int id, CancellationToken cancellationToken)
        {
            var ok = await transferenciaService.CancelarAsync(id, cancellationToken);
            return ok ? NoContent() : NotFound();
        }

        [HttpPost("{id:int}/concluir")]
        [Authorize] // role Admin?
        public async Task<IActionResult> Concluir(int id, [FromBody] ConcluirBody concluirBody, CancellationToken cancellationToken)
        {
            var ok = await transferenciaService.ConcluirAsync(id, concluirBody.Taxa, concluirBody.ValorFinal, cancellationToken);
            return ok ? NoContent() : NotFound();
        }

        public sealed class ConcluirBody
        {
            public decimal? Taxa { get; set; }
            public decimal? ValorFinal { get; set; }
        }
    }
}
