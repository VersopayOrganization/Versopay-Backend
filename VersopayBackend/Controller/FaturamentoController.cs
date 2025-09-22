using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VersopayBackend.Dtos;
using VersopayBackend.Services;

namespace VersopayBackend.Controllers
{
    [ApiController]
    [Authorize] // remova se quiser público
    [Route("api/[controller]")]
    public class FaturamentoController(IFaturamentoService service) : ControllerBase
    {
        [HttpGet("{id:int}")]
        public async Task<ActionResult<FaturamentoDto>> GetById(int id, CancellationToken ct)
        {
            var dto = await service.GetByIdAsync(id, ct);
            return dto is null ? NotFound() : Ok(dto);
        }

        /// <summary>
        /// Lista faturamentos (opcionalmente por período). Aceita CPF/CNPJ (com ou sem máscara).
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FaturamentoDto>>> Listar(
            [FromQuery] string cpfCnpj,
            [FromQuery] DateTime? inicioUtc,
            [FromQuery] DateTime? fimUtc,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(cpfCnpj))
                return BadRequest(new { message = "Informe cpfCnpj." });

            var list = await service.ListarAsync(cpfCnpj, inicioUtc, fimUtc, ct);
            return Ok(list);
        }

        /// <summary>
        /// Recalcula o faturamento para um período e CPF/CNPJ (11 ou 14 dígitos).
        /// Se Salvar=true, persiste e retorna 201; caso contrário, apenas calcula e retorna 200.
        /// </summary>
        [HttpPost("recalcular")]
        public async Task<ActionResult<FaturamentoDto>> Recalcular(
            [FromBody] FaturamentoRecalcularRequest body,
            CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            try
            {
                var dto = await service.RecalcularAsync(body, ct);
                return body.Salvar
                    ? CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto)
                    : Ok(dto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
