using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public sealed class BypassTokenRepository(AppDbContext db) : IBypassTokenRepository
    {
        public Task<BypassToken?> GetByHashWithUserAsync(string tokenHash, CancellationToken ct) =>
            db.BypassTokens
              .Include(x => x.Usuario)
              .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, ct);

        public async Task AddAsync(BypassToken entity, CancellationToken ct)
        {
            await db.BypassTokens.AddAsync(entity, ct);
        }

        public async Task RevokeAllByUserAsync(int usuarioId, DateTime whenUtc, CancellationToken ct)
        {
            var q = db.BypassTokens.Where(x => x.UsuarioId == usuarioId && x.RevogadoEmUtc == null && x.ExpiraEmUtc > whenUtc);
            await q.ExecuteUpdateAsync(s => s.SetProperty(t => t.RevogadoEmUtc, whenUtc), ct);
        }

        public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    }
}

