using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public sealed class UsuarioRepository(AppDbContext db) : IUsuarioRepository
    {
        public Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct) =>
            db.Usuarios.FirstOrDefaultAsync(u => u.Email == email, ct);

        public Task<RefreshToken?> GetRefreshWithUserByHashAsync(string tokenHash, CancellationToken ct) =>
            db.RefreshTokens.Include(r => r.Usuario).FirstOrDefaultAsync(r => r.TokenHash == tokenHash, ct);

        public Task AddRefreshAsync(RefreshToken token, CancellationToken ct)
            => db.RefreshTokens.AddAsync(token, ct).AsTask();

        public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    }
}
