using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public interface IDocumentoRepository
    {
        Task<Usuario?> GetUsuarioAsync(int usuarioId, CancellationToken ct);
        Task<Documento?> GetDocumentoAsync(int usuarioId, CancellationToken ct, bool track = true);
        Task AddDocumentoAsync(Documento doc, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}