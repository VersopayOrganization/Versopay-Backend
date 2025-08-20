using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public interface IDocumentoRepository
    {
        Task<Usuario?> GetUsuarioAsync(int usuarioId, CancellationToken cancellationToken);
        Task<Documento?> GetDocumentoAsync(int usuarioId, CancellationToken cancellationToken, bool track = true);
        Task AddDocumentoAsync(Documento documento, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}