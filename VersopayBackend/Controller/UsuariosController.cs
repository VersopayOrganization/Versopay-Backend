using Microsoft.AspNetCore.Mvc;
using VersopayBackend.Dtos;
using VersopayBackend.Services;

namespace VersopayBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController(IUsuariosService svc) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<UsuarioResponseDto>> Create([FromBody] UsuarioCreateDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try
            {
                var res = await svc.CreateAsync(dto, ct);
                return CreatedAtAction(nameof(GetById), new { id = res.Id }, res);
            }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); } // email/cpfcnpj duplicado
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); } // validação PF/PJ/CPF/CNPJ
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioResponseDto>>> GetAll(CancellationToken ct)
        {
            var res = await svc.GetAllAsync(ct);
            return Ok(res);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<UsuarioResponseDto>> GetById(int id, CancellationToken ct)
        {
            var res = await svc.GetByIdAsync(id, ct);
            return res is null ? NotFound() : Ok(res);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<UsuarioResponseDto>> Update(int id, [FromBody] UsuarioUpdateDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try
            {
                var res = await svc.UpdateAsync(id, dto, ct);
                return res is null ? NotFound() : Ok(res);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }
    }
}
