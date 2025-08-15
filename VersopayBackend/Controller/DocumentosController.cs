using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using VersopayBackend.Dtos;
using VersopayDatabase.Data;
using VersopayLibrary.Models;

namespace VersopayBackend.Controllers
{
    [ApiController]
    [Route("api/usuarios/{usuarioId:guid}/documentos")]
    public class DocumentosController(AppDbContext db, IWebHostEnvironment env, IConfiguration config) : ControllerBase
    {
        private static readonly string[] AllowedImageTypes = ["image/jpeg"]; // estrito a JPG
        private const string PdfType = "application/pdf";
        private const long MaxImageBytes = 5L * 1024 * 1024; // 5 MB
        private const long MaxPdfBytes = 10L * 1024 * 1024;  // 10 MB

        [HttpGet]
        public async Task<ActionResult<DocumentoResponseDto>> Get(Guid usuarioId)
        {
            var doc = await db.Documentos.AsNoTracking().FirstOrDefaultAsync(d => d.UsuarioId == usuarioId);
            if (doc is null) return NotFound();

            return Ok(ToDto(doc));
        }

        // cria/atualiza (envie multipart/form-data)
        [HttpPost]
        [RequestSizeLimit(MaxImageBytes * 3 + MaxPdfBytes)]
        public async Task<ActionResult<DocumentoResponseDto>> Upload(Guid usuarioId, [FromForm] DocumentoUploadDto form)
        {
            var user = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);
            if (user is null) return NotFound(new { message = "Usuário não encontrado." });

            // Valida PJ -> CartaoCnpj obrigatório
            if (user.TipoCadastro == TipoCadastro.PJ && form.CartaoCnpjPdf is null)
                return ValidationProblem("Cartão CNPJ (PDF) é obrigatório para PJ.");

            // pasta: wwwroot/uploads/{usuarioId}
            var uploadsRoot = config["Uploads:Root"] ?? "wwwroot/uploads";
            var saveRoot = Path.Combine(env.ContentRootPath, uploadsRoot, usuarioId.ToString());
            Directory.CreateDirectory(saveRoot);

            // Carrega/Cria registro
            var doc = await db.Documentos.FirstOrDefaultAsync(d => d.UsuarioId == usuarioId)
                      ?? new Documento { UsuarioId = usuarioId };

            // Salva cada arquivo se enviado
            if (form.FrenteDoc is not null)
                doc.FrenteRgCnhPath = await SaveFileAsync(form.FrenteDoc, saveRoot, AllowedImageTypes, MaxImageBytes, "frente");

            if (form.VersoDoc is not null)
                doc.VersoRgCnhPath = await SaveFileAsync(form.VersoDoc, saveRoot, AllowedImageTypes, MaxImageBytes, "verso");

            if (form.SelfieDoc is not null)
                doc.SelfieComDocPath = await SaveFileAsync(form.SelfieDoc, saveRoot, AllowedImageTypes, MaxImageBytes, "selfie");

            if (form.CartaoCnpjPdf is not null)
                doc.CartaoCnpjPdfPath = await SaveFileAsync(form.CartaoCnpjPdf, saveRoot, [PdfType], MaxPdfBytes, "cnpj");

            doc.UploadedAt = DateTime.UtcNow;

            if (db.Entry(doc).State == EntityState.Detached)
                db.Documentos.Add(doc);

            await db.SaveChangesAsync();

            return Ok(ToDto(doc));
        }

        // Helpers
        private DocumentoResponseDto ToDto(Documento d)
        {
            string? RelToUrl(string? rel)
            {
                if (string.IsNullOrWhiteSpace(rel)) return null;
                // Se estiver em wwwroot, UseStaticFiles já serve sob "/"
                return "/" + rel.Replace('\\', '/').TrimStart('/');
            }

            return new DocumentoResponseDto
            {
                UsuarioId = d.UsuarioId,
                FrenteUrl = RelToUrl(d.FrenteRgCnhPath),
                VersoUrl = RelToUrl(d.VersoRgCnhPath),
                SelfieUrl = RelToUrl(d.SelfieComDocPath),
                CartaoCnpjUrl = RelToUrl(d.CartaoCnpjPdfPath),
                UploadedAt = d.UploadedAt
            };
        }

        private static async Task<string> SaveFileAsync(IFormFile file, string folderAbs, string[] allowedTypes, long maxBytes, string prefix)
        {
            if (file.Length == 0 || file.Length > maxBytes)
                throw new InvalidOperationException("Arquivo inválido ou excede o tamanho máximo permitido.");

            if (!allowedTypes.Contains(file.ContentType))
                throw new InvalidOperationException("Tipo de arquivo não permitido.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            // força extensões padrão
            if (file.ContentType == "image/jpeg") ext = ".jpg";
            else if (file.ContentType == "application/pdf") ext = ".pdf";

            var fileName = $"{prefix}-{Guid.NewGuid():N}{ext}";
            var absPath = Path.Combine(folderAbs, fileName);

            await using var stream = System.IO.File.Create(absPath);
            await file.CopyToAsync(stream);

            // Retorna caminho relativo a wwwroot (ex.: "uploads/{uid}/file.jpg")
            var relRoot = absPath.Split(Path.DirectorySeparatorChar)
                                 .Reverse()
                                 .SkipWhile(seg => !string.Equals(seg, "wwwroot", StringComparison.OrdinalIgnoreCase))
                                 .Reverse()
                                 .ToArray();
            var idx = Array.IndexOf(relRoot, "wwwroot");
            var relative = Path.Combine(relRoot.Skip(idx + 1).ToArray());
            return relative;
        }
    }
}
