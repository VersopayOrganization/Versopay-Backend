using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using VersopayBackend.Dtos;
using VersopayBackend.Repositories;
using VersopayLibrary.Models;

namespace VersopayBackend.Services
{
    public sealed class DocumentosService(
        IDocumentoRepository repo,
        //IBlobStorageService blobSvc,
        IConfiguration cfg) : IDocumentosService
    {
        private readonly string _container = cfg["Blob:Container"] ?? "kyc-docs";

        // 1) SAS de upload (retorna lista [{ part, uploadUrl, blobName, expiresAt }])
        public async Task<IEnumerable<object>> GenerateUploadUrlsAsync(int usuarioId, UploadUrlsRequest req, CancellationToken ct)
        {
            var user = await repo.GetUsuarioAsync(usuarioId, ct)
                       ?? throw new KeyNotFoundException("Usuário não encontrado.");

            var ttl = TimeSpan.FromMinutes(10);
            var items = new List<object>();

            foreach (var part in req.Parts ?? Array.Empty<string>())
            {
                var ext = part.Equals("cnpj", StringComparison.OrdinalIgnoreCase) ? ".pdf" : ".jpg";
                var blobName = $"usuarios/{usuarioId}/{part}-{Guid.NewGuid():N}{ext}";

                //var (uri, name) = blobSvc.GetUploadSas(_container, blobName, ttl,
                //    BlobSasPermissions.Create | BlobSasPermissions.Write);

                //items.Add(new
                //{
                //    part,
                //    uploadUrl = uri.ToString(),
                //    blobName = name,
                //    expiresAt = DateTimeOffset.UtcNow.Add(ttl)
                //});
            }
            return items;
        }

        // 2) Confirmar metadados
        public async Task ConfirmAsync(int usuarioId, ConfirmDocumentoDto dto, CancellationToken ct)
        {
            var user = await repo.GetUsuarioAsync(usuarioId, ct)
                       ?? throw new KeyNotFoundException("Usuário não encontrado.");

            if (user.TipoCadastro == TipoCadastro.PJ && string.IsNullOrWhiteSpace(dto.CartaoCnpjCaminho))
                throw new InvalidOperationException("Cartão CNPJ é obrigatório para PJ.");

            var doc = await repo.GetDocumentoAsync(usuarioId, ct) ?? new Documento { UsuarioId = usuarioId };

            if (!string.IsNullOrWhiteSpace(dto.FrenteRgCaminho)) { doc.FrenteRgCaminho = dto.FrenteRgCaminho; doc.FrenteRgStatus = StatusDocumento.EmAnalise; }
            if (!string.IsNullOrWhiteSpace(dto.VersoRgCaminho)) { doc.VersoRgCaminho = dto.VersoRgCaminho; doc.VersoRgStatus = StatusDocumento.EmAnalise; }
            if (!string.IsNullOrWhiteSpace(dto.SelfieDocCaminho)) { doc.SelfieDocCaminho = dto.SelfieDocCaminho; doc.SelfieDocStatus = StatusDocumento.EmAnalise; }
            if (!string.IsNullOrWhiteSpace(dto.CartaoCnpjCaminho)) { doc.CartaoCnpjCaminho = dto.CartaoCnpjCaminho; doc.CartaoCnpjStatus = StatusDocumento.EmAnalise; }

            doc.DataAtualizacao = DateTime.UtcNow;

            if (await repo.GetDocumentoAsync(usuarioId, ct, track: false) is null)
                await repo.AddDocumentoAsync(doc, ct);

            await repo.SaveChangesAsync(ct);
        }

        // 3) URLs de leitura — retorna seu DocumentoResponseDto (URLs + expiração)
        public async Task<DocumentoResponseDto?> GetReadUrlsAsync(int usuarioId, CancellationToken ct)
        {
            var doc = await repo.GetDocumentoAsync(usuarioId, ct, track: false);
            if (doc is null) return null;

            var ttl = TimeSpan.FromMinutes(5);
            return new DocumentoResponseDto
            {
                UsuarioId = usuarioId,
                //FrenteUrl = MakeReadUrl(doc.FrenteRgCaminho, ttl),
                //VersoUrl = MakeReadUrl(doc.VersoRgCaminho, ttl),
                //SelfieUrl = MakeReadUrl(doc.SelfieDocCaminho, ttl),
                //CartaoCnpjUrl = MakeReadUrl(doc.CartaoCnpjCaminho, ttl),
                //UrlsExpiramEm = DateTime.UtcNow.Add(ttl),

                FrenteRgStatus = doc.FrenteRgStatus,
                VersoRgStatus = doc.VersoRgStatus,
                SelfieDocStatus = doc.SelfieDocStatus,
                CartaoCnpjStatus = doc.CartaoCnpjStatus,
                FrenteRgAssinaturaSha256 = doc.FrenteRgAssinaturaSha256,
                VersoRgAssinaturaSha256 = doc.VersoRgAssinaturaSha256,
                SelfieDocAssinaturaSha256 = doc.SelfieDocAssinaturaSha256,
                CartaoCnpjAssinaturaSha256 = doc.CartaoCnpjAssinaturaSha256,
                UploadedAt = doc.DataAtualizacao
            };
        }

        // 4) Status agregado — também retorna DocumentoResponseDto (URLs nulas)
        public async Task<DocumentoResponseDto?> GetStatusAsync(int usuarioId, CancellationToken ct)
        {
            var doc = await repo.GetDocumentoAsync(usuarioId, ct, track: false);
            if (doc is null) return null;

            return new DocumentoResponseDto
            {
                UsuarioId = usuarioId,
                FrenteRgStatus = doc.FrenteRgStatus,
                VersoRgStatus = doc.VersoRgStatus,
                SelfieDocStatus = doc.SelfieDocStatus,
                CartaoCnpjStatus = doc.CartaoCnpjStatus,
                FrenteRgAssinaturaSha256 = doc.FrenteRgAssinaturaSha256,
                VersoRgAssinaturaSha256 = doc.VersoRgAssinaturaSha256,
                SelfieDocAssinaturaSha256 = doc.SelfieDocAssinaturaSha256,
                CartaoCnpjAssinaturaSha256 = doc.CartaoCnpjAssinaturaSha256,
                UploadedAt = doc.DataAtualizacao
            };
        }

        // 5) Upload multipart — devolve DocumentoResponseDto com URLs temporárias
        public async Task<DocumentoResponseDto?> FormUploadAsync(int usuarioId, DocumentoUploadDto form, CancellationToken ct)
        {
            var user = await repo.GetUsuarioAsync(usuarioId, ct)
                       ?? throw new KeyNotFoundException("Usuário não encontrado.");

            if (user.TipoCadastro == TipoCadastro.PJ && form.CartaoCnpjPdf is null)
                throw new InvalidOperationException("Cartão CNPJ (PDF) é obrigatório para PJ.");

            const long MAX_IMG = 5L * 1024 * 1024;  // 5MB
            const long MAX_PDF = 10L * 1024 * 1024; // 10MB

            static void CheckImg(IFormFile? f, string nome)
            {
                if (f is null) return;
                if (f.Length == 0 || f.Length > MAX_IMG) throw new InvalidOperationException($"{nome}: tamanho inválido.");
                if (f.ContentType != "image/jpeg") throw new InvalidOperationException($"{nome}: apenas image/jpeg.");
            }
            static void CheckPdf(IFormFile? f, string nome)
            {
                if (f is null) return;
                if (f.Length == 0 || f.Length > MAX_PDF) throw new InvalidOperationException($"{nome}: tamanho inválido.");
                if (f.ContentType != "application/pdf") throw new InvalidOperationException($"{nome}: apenas application/pdf.");
            }

            CheckImg(form.FrenteDoc, "FrenteDoc");
            CheckImg(form.VersoDoc, "VersoDoc");
            CheckImg(form.SelfieDoc, "SelfieDoc");
            CheckPdf(form.CartaoCnpjPdf, "CartaoCnpjPdf");

            var doc = await repo.GetDocumentoAsync(usuarioId, ct) ?? new Documento { UsuarioId = usuarioId };

            var toUpload = new List<(IFormFile file, string part, string ext)>();
            if (form.FrenteDoc is not null) toUpload.Add((form.FrenteDoc, "frente", ".jpg"));
            if (form.VersoDoc is not null) toUpload.Add((form.VersoDoc, "verso", ".jpg"));
            if (form.SelfieDoc is not null) toUpload.Add((form.SelfieDoc, "selfie", ".jpg"));
            if (form.CartaoCnpjPdf is not null) toUpload.Add((form.CartaoCnpjPdf, "cnpj", ".pdf"));

            foreach (var (file, part, ext) in toUpload)
            {
                var blobName = $"usuarios/{usuarioId}/{part}-{Guid.NewGuid():N}{ext}";
                //var (sasUri, _) = blobSvc.GetUploadSas(_container, blobName, TimeSpan.FromMinutes(10),
                //    BlobSasPermissions.Create | BlobSasPermissions.Write);

                //var blob = new BlobClient(sasUri);
                await using var s = file.OpenReadStream();
                //await blob.UploadAsync(s, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType } });

                var caminho = $"{_container}/{blobName}";
                switch (part)
                {
                    case "frente": doc.FrenteRgCaminho = caminho; doc.FrenteRgStatus = StatusDocumento.EmAnalise; break;
                    case "verso": doc.VersoRgCaminho = caminho; doc.VersoRgStatus = StatusDocumento.EmAnalise; break;
                    case "selfie": doc.SelfieDocCaminho = caminho; doc.SelfieDocStatus = StatusDocumento.EmAnalise; break;
                    case "cnpj": doc.CartaoCnpjCaminho = caminho; doc.CartaoCnpjStatus = StatusDocumento.EmAnalise; break;
                }
                doc.DataAtualizacao = DateTime.UtcNow;
            }

            if (await repo.GetDocumentoAsync(usuarioId, ct, track: false) is null)
                await repo.AddDocumentoAsync(doc, ct);

            await repo.SaveChangesAsync(ct);

            // monta resposta com URLs temporárias
            var ttl = TimeSpan.FromMinutes(5);
            return new DocumentoResponseDto
            {
                UsuarioId = usuarioId,
                //FrenteUrl = MakeReadUrl(doc.FrenteRgCaminho, ttl),
                //VersoUrl = MakeReadUrl(doc.VersoRgCaminho, ttl),
                //SelfieUrl = MakeReadUrl(doc.SelfieDocCaminho, ttl),
                //CartaoCnpjUrl = MakeReadUrl(doc.CartaoCnpjCaminho, ttl),
                //UrlsExpiramEm = DateTime.UtcNow.Add(ttl),

                FrenteRgStatus = doc.FrenteRgStatus,
                VersoRgStatus = doc.VersoRgStatus,
                SelfieDocStatus = doc.SelfieDocStatus,
                CartaoCnpjStatus = doc.CartaoCnpjStatus,
                FrenteRgAssinaturaSha256 = doc.FrenteRgAssinaturaSha256,
                VersoRgAssinaturaSha256 = doc.VersoRgAssinaturaSha256,
                SelfieDocAssinaturaSha256 = doc.SelfieDocAssinaturaSha256,
                CartaoCnpjAssinaturaSha256 = doc.CartaoCnpjAssinaturaSha256,
                UploadedAt = doc.DataAtualizacao
            };
        }

        //private string? MakeReadUrl(string? caminho, TimeSpan ttl)
        //{
        //    if (string.IsNullOrWhiteSpace(caminho)) return null;
        //    var (cont, name) = SplitStoragePath(caminho, _container);
        //    return blobSvc.GetReadSas(cont, name, ttl).ToString();
        //}

        private static (string container, string name) SplitStoragePath(string caminho, string defaultContainer)
        {
            var idx = caminho.IndexOf('/');
            return idx > 0 ? (caminho[..idx], caminho[(idx + 1)..]) : (defaultContainer, caminho);
        }
    }
}