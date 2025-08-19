using VersopayBackend.Dtos;

namespace VersopayBackend.Services
{
    public interface IDocumentosService
    {
        Task<IEnumerable<object>> GenerateUploadUrlsAsync(int usuarioId, UploadUrlsRequest req, CancellationToken ct);
        Task ConfirmAsync(int usuarioId, ConfirmDocumentoDto dto, CancellationToken ct);
        Task<DocumentoResponseDto?> GetReadUrlsAsync(int usuarioId, CancellationToken ct);
        Task<DocumentoResponseDto?> GetStatusAsync(int usuarioId, CancellationToken ct);
        Task<DocumentoResponseDto?> FormUploadAsync(int usuarioId, DocumentoUploadDto form, CancellationToken ct);
    }
}