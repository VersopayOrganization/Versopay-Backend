using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public sealed class DocumentoRepository(AppDbContext appDbContext) : IDocumentoRepository
    {
        public Task<Usuario?> GetUsuarioAsync(int usuarioId, CancellationToken ct) =>
            appDbContext.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId, ct);

        public async Task<Documento?> GetDocumentoAsync(int usuarioId, CancellationToken ct, bool track = true)
        {
            var q = track ? appDbContext.Documentos : appDbContext.Documentos.AsNoTracking();
            return await q.FirstOrDefaultAsync(d => d.UsuarioId == usuarioId, ct);
        }

        public Task AddDocumentoAsync(Documento doc, CancellationToken ct) =>
            appDbContext.Documentos.AddAsync(doc, ct).AsTask();

        public Task SaveChangesAsync(CancellationToken ct) => appDbContext.SaveChangesAsync(ct);
    }
}