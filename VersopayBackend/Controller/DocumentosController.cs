using Microsoft.AspNetCore.Mvc;
using VersopayBackend.Dtos;
using VersopayBackend.Services;

namespace VersopayBackend.Controllers
{
    [ApiController]
    [Route("api/documentos/{usuarioId:int}")]
    public class DocumentosController(IDocumentosService documentoService) : ControllerBase
    {
        [HttpPost("upload-urls")]
        public async Task<ActionResult<IEnumerable<object>>> GetUploadUrls(int usuarioId, [FromBody] UploadUrlsRequest uploadUrl, CancellationToken cancellationToken)
        {
            try
            {
                var response = await documentoService.GenerateUploadUrlsAsync(usuarioId, uploadUrl, cancellationToken);
                return Ok(response);
            }
            catch (KeyNotFoundException) { return NotFound(new { message = "Usuário não encontrado." }); }
        }

        [HttpPost("confirm")]
        public async Task<IActionResult> Confirm(int usuarioId, [FromBody] ConfirmDocumentoDto confirmDocumentoDto, CancellationToken cancellationToken)
        {
            try
            {
                await documentoService.ConfirmAsync(usuarioId, confirmDocumentoDto, cancellationToken);
                return NoContent();
            }
            catch (KeyNotFoundException) { return NotFound(new { message = "Usuário não encontrado." }); }
            catch (InvalidOperationException exception) { return BadRequest(new { message = exception.Message }); }
        }

        [HttpGet("urls")]
        public async Task<ActionResult<DocumentoResponseDto>> GetReadUrls(int usuarioId, CancellationToken cancellationToken)
        {
            var documentoResponse = await documentoService.GetReadUrlsAsync(usuarioId, cancellationToken);
            return documentoResponse is null ? NotFound() : Ok(documentoResponse);
        }

        [HttpGet("status")]
        public async Task<ActionResult<DocumentoResponseDto>> GetStatus(int usuarioId, CancellationToken cancellationToken)
        {
            var documentoResponse = await documentoService.GetStatusAsync(usuarioId, cancellationToken);
            return documentoResponse is null ? NotFound() : Ok(documentoResponse);
        }

        [HttpPost("form-upload")]
        [RequestSizeLimit(30L * 1024 * 1024)]
        public async Task<ActionResult<DocumentoResponseDto>> FormUpload(int usuarioId, [FromForm] DocumentoUploadDto form, CancellationToken cancellationToken)
        {
            try
            {
                var response = await documentoService.FormUploadAsync(usuarioId, form, cancellationToken);
                return Ok(response);
            }
            catch (KeyNotFoundException) { return NotFound(new { message = "Usuário não encontrado." }); }
            catch (InvalidOperationException exception) { return BadRequest(new { message = exception.Message }); }
        }
    }
}