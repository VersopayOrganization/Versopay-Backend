using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public sealed class DocumentoRepository(AppDbContext db) : IDocumentoRepository
    {
        public Task<Usuario?> GetUsuarioAsync(int usuarioId, CancellationToken ct) =>
            db.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId, ct);

        public async Task<Documento?> GetDocumentoAsync(int usuarioId, CancellationToken ct, bool track = true)
        {
            var q = track ? db.Documentos : db.Documentos.AsNoTracking();
            return await q.FirstOrDefaultAsync(d => d.UsuarioId == usuarioId, ct);
        }

        public Task AddDocumentoAsync(Documento doc, CancellationToken ct) =>
            db.Documentos.AddAsync(doc, ct).AsTask();

        public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    }
}