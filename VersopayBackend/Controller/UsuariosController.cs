using Microsoft.AspNetCore.Mvc;
using VersopayBackend.Dtos;
using VersopayBackend.Services;

namespace VersopayBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController(IUsuariosService usuarioService) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<UsuarioResponseDto>> Create([FromBody] UsuarioCreateDto usuarioCreateDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try
            {
                var response = await usuarioService.CreateAsync(usuarioCreateDto, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
            }
            catch (InvalidOperationException exception) { return Conflict(new { message = exception.Message }); } // email/cpfcnpj duplicado
            catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); } // validação PF/PJ/CPF/CNPJ
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioResponseDto>>> GetAll(CancellationToken cancellationToken)
        {
            var response = await usuarioService.GetAllAsync(cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<UsuarioResponseDto>> GetById(int id, CancellationToken cancellationToken)
        {
            var response = await usuarioService.GetByIdAsync(id, cancellationToken);
            return response is null ? NotFound() : Ok(response);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<UsuarioResponseDto>> Update(int id, [FromBody] UsuarioUpdateDto usuarioUpdateDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try
            {
                var response = await usuarioService.UpdateAsync(id, usuarioUpdateDto, cancellationToken);
                return response is null ? NotFound() : Ok(response);
            }
            catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        }
    }
}
