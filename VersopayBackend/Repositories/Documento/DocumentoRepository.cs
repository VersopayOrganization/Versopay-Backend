using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public sealed class DocumentoRepository(AppDbContext appDbContext) : IDocumentoRepository
    {
        public Task<Usuario?> GetUsuarioAsync(int usuarioId, CancellationToken cancellationToken) =>
            appDbContext.Usuarios.FirstOrDefaultAsync(usuario => usuario.Id == usuarioId, cancellationToken);

        public async Task<Documento?> GetDocumentoAsync(int usuarioId, CancellationToken cancellationToken, bool track = true)
        {
            var query = track ? appDbContext.Documentos : appDbContext.Documentos.AsNoTracking();
            return await query.FirstOrDefaultAsync(documento => documento.UsuarioId == usuarioId, cancellationToken);
        }

        public Task AddDocumentoAsync(Documento documento, CancellationToken cancellationToken) =>
            appDbContext.Documentos.AddAsync(documento, cancellationToken).AsTask();

        public Task SaveChangesAsync(CancellationToken cancellationToken) => appDbContext.SaveChangesAsync(cancellationToken);
    }
}