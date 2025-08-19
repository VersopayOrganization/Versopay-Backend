using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VersopayBackend.Dtos;
using VersopayBackend.Services;
using VersopayDatabase.Data;
using VersopayLibrary.Models;

namespace VersopayBackend.Controllers
{
    [ApiController]
    [Route("api/usuarios/{usuarioId:int}/documentos")]
    public class DocumentosController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IBlobStorageService _blobSvc;
        private readonly string _container;

        public DocumentosController(AppDbContext db, IBlobStorageService blobSvc, IConfiguration cfg)
        {
            _db = db;
            _blobSvc = blobSvc;
            _container = cfg["Blob:Container"] ?? "kyc-docs";
        }

        // ---------- 1) Gerar SAS de upload ----------
        public record UploadUrlsRequest(string[] Parts); // "frente","verso","selfie","cnpj"
        public record UploadItem(string Part, string UploadUrl, string BlobName, DateTimeOffset ExpiresAt);
        public record UploadUrlsResponse(List<UploadItem> Items);

        [HttpPost("upload-urls")]
        public async Task<ActionResult<UploadUrlsResponse>> GetUploadUrls(int usuarioId, [FromBody] UploadUrlsRequest req)
        {
            var user = await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == usuarioId);
            if (user is null)
                return NotFound(new { message = "Usuário não encontrado." });

            var ttl = TimeSpan.FromMinutes(10);
            var items = new List<UploadItem>();

            foreach (var part in req.Parts ?? Array.Empty<string>())
            {
                var ext = part.Equals("cnpj", StringComparison.OrdinalIgnoreCase) ? ".pdf" : ".jpg";
                var blobName = $"usuarios/{usuarioId}/{part}-{Guid.NewGuid():N}{ext}";
                var (uri, name) = _blobSvc.GetUploadSas(_container, blobName, ttl,
                    BlobSasPermissions.Create | BlobSasPermissions.Write);

                items.Add(new UploadItem(part, uri.ToString(), name, DateTimeOffset.UtcNow.Add(ttl)));
            }

            return Ok(new UploadUrlsResponse(items));
        }

        // ---------- 2) Confirmar metadados ----------
        public record ConfirmDto(string? FrenteRgCaminho, string? VersoRgCaminho, string? SelfieDocCaminho, string? CartaoCnpjCaminho);

        [HttpPost("confirm")]
        public async Task<IActionResult> Confirm(int usuarioId, [FromBody] ConfirmDto dto)
        {
            var user = await _db.Usuarios.Include(u => u.Documento).FirstOrDefaultAsync(u => u.Id == usuarioId);
            if (user is null)
                return NotFound(new { message = "Usuário não encontrado." });

            if (user.TipoCadastro == TipoCadastro.PJ && string.IsNullOrWhiteSpace(dto.CartaoCnpjCaminho))
                return BadRequest(new { message = "Cartão CNPJ é obrigatório para PJ." });

            var doc = user.Documento ?? new Documento { UsuarioId = usuarioId };

            if (!string.IsNullOrWhiteSpace(dto.FrenteRgCaminho))
            {
                doc.FrenteRgCaminho = dto.FrenteRgCaminho;
                doc.FrenteRgStatus = StatusDocumento.EmAnalise;
            }

            if (!string.IsNullOrWhiteSpace(dto.VersoRgCaminho))
            {
                doc.VersoRgCaminho = dto.VersoRgCaminho;
                doc.VersoRgStatus = StatusDocumento.EmAnalise;
            }

            if (!string.IsNullOrWhiteSpace(dto.SelfieDocCaminho))
            {
                doc.SelfieDocCaminho = dto.SelfieDocCaminho;
                doc.SelfieDocStatus = StatusDocumento.EmAnalise;
            }

            if (!string.IsNullOrWhiteSpace(dto.CartaoCnpjCaminho))
            {
                doc.CartaoCnpjCaminho = dto.CartaoCnpjCaminho;
                doc.CartaoCnpjStatus = StatusDocumento.EmAnalise;
            }

            doc.DataAtualizacao = DateTime.UtcNow;

            if (user.Documento is null) _db.Documentos.Add(doc);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // ---------- 3) Obter SAS de leitura (URLs temporárias) ----------
        public record ReadUrlsResponse(string? FrenteUrl, string? VersoUrl, string? SelfieUrl, string? CartaoCnpjUrl, DateTimeOffset ExpiresAt);

        [HttpGet("urls")]
        public async Task<ActionResult<ReadUrlsResponse>> GetReadUrls(int usuarioId)
        {
            var user = await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == usuarioId);
            if (user is null) return NotFound();

            var doc = await _db.Documentos.AsNoTracking().FirstOrDefaultAsync(d => d.UsuarioId == usuarioId);
            if (doc is null) return NotFound();

            var ttl = TimeSpan.FromMinutes(5);

            var res = new ReadUrlsResponse(
                FrenteUrl: MakeReadUrl(doc.FrenteRgCaminho, ttl),
                VersoUrl: MakeReadUrl(doc.VersoRgCaminho, ttl),
                SelfieUrl: MakeReadUrl(doc.SelfieDocCaminho, ttl),
                CartaoCnpjUrl: MakeReadUrl(doc.CartaoCnpjCaminho, ttl),
                ExpiresAt: DateTimeOffset.UtcNow.Add(ttl)
            );
            return Ok(res);
        }

        // ---------- 4) Status agregado ----------
        [HttpGet("status")]
        public async Task<ActionResult<object>> GetStatus(int usuarioId)
        {
            var doc = await _db.Documentos.AsNoTracking().FirstOrDefaultAsync(d => d.UsuarioId == usuarioId);
            if (doc is null) return NotFound();

            var requerCnpj = await _db.Usuarios.Where(u => u.Id == usuarioId)
                                               .Select(u => u.TipoCadastro == TipoCadastro.PJ)
                                               .FirstAsync();

            bool okFrente = doc.FrenteRgStatus == StatusDocumento.Verificado;
            bool okVerso = doc.VersoRgStatus == StatusDocumento.Verificado;
            bool okSelfie = doc.SelfieDocStatus == StatusDocumento.Verificado;
            bool okCnpj = !requerCnpj || doc.CartaoCnpjStatus == StatusDocumento.Verificado;

            var statusGeral =
                (okFrente && okVerso && okSelfie && okCnpj) ? "Verificado" :
                new[] { doc.FrenteRgStatus, doc.VersoRgStatus, doc.SelfieDocStatus, doc.CartaoCnpjStatus }.Any(s => s == StatusDocumento.Rejeitado) ? "Rejeitado" :
                new[] { doc.FrenteRgStatus, doc.VersoRgStatus, doc.SelfieDocStatus, doc.CartaoCnpjStatus }.Any(s => s == StatusDocumento.EmAnalise) ? "EmAnalise" :
                "Pendente";

            return Ok(new
            {
                doc.UsuarioId,
                doc.FrenteRgStatus,
                doc.VersoRgStatus,
                doc.SelfieDocStatus,
                doc.CartaoCnpjStatus,
                StatusGeral = statusGeral,
                doc.DataAtualizacao
            });
        }

        // ---------- 5) (Opcional) Upload via multipart para Blob ----------
        [HttpPost("form-upload")]
        [RequestSizeLimit(30L * 1024 * 1024)] // ~30MB total
        public async Task<ActionResult<DocumentoResponseDto>> FormUpload(int usuarioId, [FromForm] DocumentoUploadDto form)
        {
            try
            {
                var user = await _db.Usuarios.Include(u => u.Documento).FirstOrDefaultAsync(u => u.Id == usuarioId);
                if (user is null) return NotFound(new { message = "Usuário não encontrado." });

                if (user.TipoCadastro == TipoCadastro.PJ && form.CartaoCnpjPdf is null)
                    return BadRequest(new { message = "Cartão CNPJ (PDF) é obrigatório para PJ." });

                const long MAX_IMG = 5L * 1024 * 1024;  // 5MB
                const long MAX_PDF = 10L * 1024 * 1024; // 10MB

                void CheckImg(IFormFile? f, string nome)
                {
                    if (f is null) return;
                    if (f.Length == 0 || f.Length > MAX_IMG) throw new InvalidOperationException($"{nome}: tamanho inválido.");
                    if (f.ContentType != "image/jpeg") throw new InvalidOperationException($"{nome}: apenas image/jpeg.");
                }
                void CheckPdf(IFormFile? f, string nome)
                {
                    if (f is null) return;
                    if (f.Length == 0 || f.Length > MAX_PDF) throw new InvalidOperationException($"{nome}: tamanho inválido.");
                    if (f.ContentType != "application/pdf") throw new InvalidOperationException($"{nome}: apenas application/pdf.");
                }

                CheckImg(form.FrenteDoc, "FrenteDoc");
                CheckImg(form.VersoDoc, "VersoDoc");
                CheckImg(form.SelfieDoc, "SelfieDoc");
                CheckPdf(form.CartaoCnpjPdf, "CartaoCnpjPdf");

                var toUpload = new List<(IFormFile file, string part, string ext)>();
                if (form.FrenteDoc is not null) toUpload.Add((form.FrenteDoc, "frente", ".jpg"));
                if (form.VersoDoc is not null) toUpload.Add((form.VersoDoc, "verso", ".jpg"));
                if (form.SelfieDoc is not null) toUpload.Add((form.SelfieDoc, "selfie", ".jpg"));
                if (form.CartaoCnpjPdf is not null) toUpload.Add((form.CartaoCnpjPdf, "cnpj", ".pdf"));

                foreach (var (file, part, ext) in toUpload)
                {
                    var blobName = $"usuarios/{usuarioId}/{part}-{Guid.NewGuid():N}{ext}";
                    var (sasUri, _) = _blobSvc.GetUploadSas(_container, blobName, TimeSpan.FromMinutes(10),
                        BlobSasPermissions.Create | BlobSasPermissions.Write);

                    var blob = new BlobClient(sasUri);
                    await using var s = file.OpenReadStream();
                    var headers = new BlobHttpHeaders { ContentType = file.ContentType };
                    await blob.UploadAsync(s, new BlobUploadOptions { HttpHeaders = headers });

                    user.Documento ??= new Documento { UsuarioId = usuarioId };
                    switch (part)
                    {
                        case "frente":
                            user.Documento.FrenteRgCaminho = $"{_container}/{blobName}";
                            user.Documento.FrenteRgStatus = StatusDocumento.EmAnalise;
                            break;
                        case "verso":
                            user.Documento.VersoRgCaminho = $"{_container}/{blobName}";
                            user.Documento.VersoRgStatus = StatusDocumento.EmAnalise;
                            break;
                        case "selfie":
                            user.Documento.SelfieDocCaminho = $"{_container}/{blobName}";
                            user.Documento.SelfieDocStatus = StatusDocumento.EmAnalise;
                            break;
                        case "cnpj":
                            user.Documento.CartaoCnpjCaminho = $"{_container}/{blobName}";
                            user.Documento.CartaoCnpjStatus = StatusDocumento.EmAnalise;
                            break;
                    }
                    user.Documento.DataAtualizacao = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();

                var ttl = TimeSpan.FromMinutes(5);
                var d = user.Documento!;
                var resp = new DocumentoResponseDto
                {
                    UsuarioId = usuarioId,
                    FrenteUrl = MakeReadUrl(d.FrenteRgCaminho, ttl),
                    VersoUrl = MakeReadUrl(d.VersoRgCaminho, ttl),
                    SelfieUrl = MakeReadUrl(d.SelfieDocCaminho, ttl),
                    CartaoCnpjUrl = MakeReadUrl(d.CartaoCnpjCaminho, ttl),
                    UrlsExpiramEm = DateTime.UtcNow.Add(ttl),

                    // Se o seu DTO não tiver esses campos, remova as linhas abaixo
                    FrenteRgStatus = d.FrenteRgStatus,
                    VersoRgStatus = d.VersoRgStatus,
                    SelfieDocStatus = d.SelfieDocStatus,
                    CartaoCnpjStatus = d.CartaoCnpjStatus,
                    FrenteRgAssinaturaSha256 = d.FrenteRgAssinaturaSha256,
                    VersoRgAssinaturaSha256 = d.VersoRgAssinaturaSha256,
                    SelfieDocAssinaturaSha256 = d.SelfieDocAssinaturaSha256,
                    CartaoCnpjAssinaturaSha256 = d.CartaoCnpjAssinaturaSha256,

                    UploadedAt = d.DataAtualizacao
                };

                return Ok(resp);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ----------------- Helpers -----------------
        private static (string container, string name) SplitStoragePath(string caminho, string defaultContainer)
        {
            // Aceita "container/blobname" ou só "blobname"
            var idx = caminho.IndexOf('/');
            if (idx > 0)
                return (caminho[..idx], caminho[(idx + 1)..]);
            return (defaultContainer, caminho);
        }

        private string? MakeReadUrl(string? caminho, TimeSpan ttl)
        {
            if (string.IsNullOrWhiteSpace(caminho)) return null;
            var (cont, name) = SplitStoragePath(caminho, _container);
            return _blobSvc.GetReadSas(cont, name, ttl).ToString();
        }
    }
}
