// Controllers/DocumentosController.cs
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VersopayBackend.Services;
using VersopayDatabase.Data;
using VersopayLibrary.Models;

namespace VersopayBackend.Controllers
{
    [ApiController]
    [Route("api/usuarios/{usuarioId:int}/documentos")]
    public class DocumentosController(AppDbContext db, IBlobStorageService blobSvc, IConfiguration cfg) : ControllerBase
    {
        private readonly string _container = cfg["Blob:Container"] ?? "kyc-docs";

        // ---- 1) Gerar SAS de upload ----
        public record UploadUrlsRequest(string[] Parts); // "frente","verso","selfie","cnpj"
        public record UploadItem(string Part, string UploadUrl, string BlobName, DateTimeOffset ExpiresAt);
        public record UploadUrlsResponse(List<UploadItem> Items);

        [HttpPost("upload-urls")]
        public async Task<ActionResult<UploadUrlsResponse>> GetUploadUrls(int usuarioId, [FromBody] UploadUrlsRequest req)
        {
            var user = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == usuarioId);
            if (user is null) return NotFound(new { message = "Usuário não encontrado." });

            var ttl = TimeSpan.FromMinutes(10);
            var items = new List<UploadItem>();

            foreach (var part in req.Parts ?? Array.Empty<string>())
            {
                var ext = part.Equals("cnpj", StringComparison.OrdinalIgnoreCase) ? ".pdf" : ".jpg";
                var blobName = $"usuarios/{usuarioId}/{part}-{Guid.NewGuid():N}{ext}";
                var (uri, name) = blobSvc.GetUploadSas(_container, blobName, ttl,
                    BlobSasPermissions.Create | BlobSasPermissions.Write);

                items.Add(new UploadItem(part, uri.ToString(), name, DateTimeOffset.UtcNow.Add(ttl)));
            }

            return Ok(new UploadUrlsResponse(items));
        }

        // ---- 2) Confirmar metadados (grava nomes dos blobs) ----
        public record ConfirmDto(string? FrenteRgCaminho, string? VersoRgCaminho, string? SelfieDocCaminho, string? CartaoCnpjCaminho);

        [HttpPost("confirm")]
        public async Task<IActionResult> Confirm(int usuarioId, [FromBody] ConfirmDto dto)
        {
            var user = await db.Usuarios.Include(u => u.Documento).FirstOrDefaultAsync(u => u.Id == usuarioId);
            if (user is null) return NotFound(new { message = "Usuário não encontrado." });

            if (user.TipoCadastro == TipoCadastro.PJ && string.IsNullOrWhiteSpace(dto.CartaoCnpjCaminho))
                return ValidationProblem("Cartão CNPJ é obrigatório para PJ.");

            // cria/atualiza registro
            var doc = user.Documento ?? new Documento { UsuarioId = usuarioId };
            if (!string.IsNullOrWhiteSpace(dto.FrenteRgCaminho)) doc.FrenteRgCaminho = dto.FrenteRgCaminho;
            if (!string.IsNullOrWhiteSpace(dto.VersoRgCaminho)) doc.VersoRgCaminho = dto.VersoRgCaminho;
            if (!string.IsNullOrWhiteSpace(dto.SelfieDocCaminho)) doc.SelfieDocCaminho = dto.SelfieDocCaminho;
            if (!string.IsNullOrWhiteSpace(dto.CartaoCnpjCaminho)) doc.CartaoCnpjCaminho = dto.CartaoCnpjCaminho;
            doc.DataAtualizacao = DateTime.UtcNow;

            if (user.Documento is null) db.Documentos.Add(doc);
            await db.SaveChangesAsync();

            return NoContent();
        }

        // ---- 3) Obter SAS de leitura (URLs temporárias) ----
        public record ReadUrlsResponse(string? FrenteUrl, string? VersoUrl, string? SelfieUrl, string? CartaoCnpjUrl, DateTimeOffset ExpiresAt);

        [HttpGet("urls")]
        public async Task<ActionResult<ReadUrlsResponse>> GetReadUrls(int usuarioId)
        {
            var user = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == usuarioId);
            if (user is null) return NotFound();

            var doc = await db.Documentos.AsNoTracking().FirstOrDefaultAsync(d => d.UsuarioId == usuarioId);
            if (doc is null) return NotFound();

            var ttl = TimeSpan.FromMinutes(5);
            Uri? mk(string? blob) => string.IsNullOrWhiteSpace(blob) ? null : blobSvc.GetReadSas(_container, blob, ttl);

            var res = new ReadUrlsResponse(
                FrenteUrl: mk(doc.FrenteRgCaminho)?.ToString(),
                VersoUrl: mk(doc.VersoRgCaminho)?.ToString(),
                SelfieUrl: mk(doc.SelfieDocCaminho)?.ToString(),
                CartaoCnpjUrl: mk(doc.CartaoCnpjCaminho)?.ToString(),
                ExpiresAt: DateTimeOffset.UtcNow.Add(ttl)
            );
            return Ok(res);
        }
    }
}
