using Microsoft.AspNetCore.Mvc;
using VersopayBackend.Dtos;
using VersopayBackend.Services;

namespace VersopayBackend.Controllers
{
    [ApiController]
    [Route("api/usuarios/{usuarioId:int}/documentos")]
    public class DocumentosController(IDocumentosService svc) : ControllerBase
    {
        [HttpPost("upload-urls")]
        public async Task<ActionResult<IEnumerable<object>>> GetUploadUrls(int usuarioId, [FromBody] UploadUrlsRequest req, CancellationToken ct)
        {
            try
            {
                var res = await svc.GenerateUploadUrlsAsync(usuarioId, req, ct);
                return Ok(res);
            }
            catch (KeyNotFoundException) { return NotFound(new { message = "Usuário não encontrado." }); }
        }

        [HttpPost("confirm")]
        public async Task<IActionResult> Confirm(int usuarioId, [FromBody] ConfirmDocumentoDto dto, CancellationToken ct)
        {
            try
            {
                await svc.ConfirmAsync(usuarioId, dto, ct);
                return NoContent();
            }
            catch (KeyNotFoundException) { return NotFound(new { message = "Usuário não encontrado." }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("urls")]
        public async Task<ActionResult<DocumentoResponseDto>> GetReadUrls(int usuarioId, CancellationToken ct)
        {
            var res = await svc.GetReadUrlsAsync(usuarioId, ct);
            return res is null ? NotFound() : Ok(res);
        }

        [HttpGet("status")]
        public async Task<ActionResult<DocumentoResponseDto>> GetStatus(int usuarioId, CancellationToken ct)
        {
            var res = await svc.GetStatusAsync(usuarioId, ct);
            return res is null ? NotFound() : Ok(res);
        }

        [HttpPost("form-upload")]
        [RequestSizeLimit(30L * 1024 * 1024)]
        public async Task<ActionResult<DocumentoResponseDto>> FormUpload(int usuarioId, [FromForm] DocumentoUploadDto form, CancellationToken ct)
        {
            try
            {
                var res = await svc.FormUploadAsync(usuarioId, form, ct);
                return Ok(res);
            }
            catch (KeyNotFoundException) { return NotFound(new { message = "Usuário não encontrado." }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }
    }
}