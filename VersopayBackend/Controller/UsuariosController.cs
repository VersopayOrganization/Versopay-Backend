using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VersopayBackend.Dtos;
using VersopayBackend.Services;
using static VersopayBackend.Dtos.PasswordResetDtos;

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

        [HttpPost("esqueci-senha")]
        [AllowAnonymous]
        public async Task<IActionResult> EsqueciSenha(
        [FromBody] SenhaEsquecidaRequest senhaEsquecidaRequest,
        [FromServices] IConfiguration config,
        CancellationToken cancellationToken)
        {
            var baseResetUrl = config["Frontend:ResetUrl"];

            var link = await usuarioService.ResetSenhaRequestAsync(
                senhaEsquecidaRequest,
                baseResetUrl,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                cancellationToken);

            return Ok(new { resetLink = link });
        }

    [HttpGet("resetar-senha/validar")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidarResetToken([FromQuery] string token, CancellationToken cancellationToken)
        {
            var ok = await usuarioService.ValidarTokenResetSenhaAsync(token, cancellationToken);
            return ok ? Ok(new { message = "Token válido" }) : BadRequest(new { message = "Token inválido ou expirado." });
        }

        [HttpPost("resetar-senha")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetarSenha([FromBody] RedefinirSenhaRequest redefinirSenhaRequest, CancellationToken cancellationToken)
        {
            var ok = await usuarioService.ResetSenhaAsync(redefinirSenhaRequest, cancellationToken);
            return ok ? Ok(new {message = "Senha redefinida com sucesso."}) : BadRequest(new { message = "Token inválido/expirado ou senhas não conferem." });
        }
    }
}
