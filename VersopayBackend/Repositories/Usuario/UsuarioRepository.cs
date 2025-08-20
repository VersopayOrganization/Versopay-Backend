using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public sealed class UsuarioRepository(AppDbContext appDbContext) : IUsuarioRepository
    {
        public Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct) =>
            appDbContext.Usuarios.FirstOrDefaultAsync(u => u.Email == email, ct);

        public Task<RefreshToken?> GetRefreshWithUserByHashAsync(string tokenHash, CancellationToken ct) =>
            appDbContext.RefreshTokens.Include(r => r.Usuario).FirstOrDefaultAsync(r => r.TokenHash == tokenHash, ct);

        public Task AddRefreshAsync(RefreshToken token, CancellationToken ct)
            => appDbContext.RefreshTokens.AddAsync(token, ct).AsTask();

        public Task SaveChangesAsync(CancellationToken ct) => appDbContext.SaveChangesAsync(ct);
    }
}
