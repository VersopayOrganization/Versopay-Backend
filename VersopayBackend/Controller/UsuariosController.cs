using Microsoft.AspNetCore.Mvc;
using VersopayBackend.Dtos;
using VersopayBackend.Services;

namespace VersopayBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController(IUsuariosService usuarioService) : ControllerBase
    {
        [HttpPost("cadastro-inicial")]
        public async Task<ActionResult<UsuarioResponseDto>> CadastroInicial([FromBody] UsuarioCreateDto usuarioCreateDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try
            {
                var response = await usuarioService.CadastroInicialAsync(usuarioCreateDto, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
            }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        }

        [HttpPut("{id:int}/completar-cadastro")]
        public async Task<ActionResult<UsuarioResponseDto>> CompletarCadastro([FromBody] UsuarioCompletarCadastroDto usuarioCompletarCadastroDto, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try
            {
                var response = await usuarioService.CompletarCadastroAsync(usuarioCompletarCadastroDto, cancellationToken);
                return response is null ? NotFound() : Ok(response);
            }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
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
