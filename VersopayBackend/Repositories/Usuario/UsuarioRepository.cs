using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public sealed class UsuarioRepository(AppDbContext appDbContext) : IUsuarioRepository
    {
        // === existentes ===
        public Task<Usuario?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
            appDbContext.Usuarios.FirstOrDefaultAsync(usuario => usuario.Email == email, cancellationToken);

        public Task<RefreshToken?> GetRefreshWithUserByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
            appDbContext.RefreshTokens.Include(refreshToken => refreshToken.Usuario).FirstOrDefaultAsync(refreshToken => refreshToken.TokenHash == tokenHash, cancellationToken);

        public Task AddRefreshAsync(RefreshToken token, CancellationToken cancellationToken) =>
            appDbContext.RefreshTokens.AddAsync(token, cancellationToken).AsTask();

        public Task SaveChangesAsync(CancellationToken cancellationToken) => appDbContext.SaveChangesAsync(cancellationToken);

        // === novos (consultas) ===
        public Task<List<Usuario>> GetAllNoTrackingAsync(CancellationToken cancellationToken) =>
            appDbContext.Usuarios.AsNoTracking()
              .OrderByDescending(usuario => usuario.DataCriacao)
              .ToListAsync(cancellationToken);

        public Task<Usuario?> GetByIdNoTrackingAsync(int id, CancellationToken cancellationToken) =>
            appDbContext.Usuarios.AsNoTracking().FirstOrDefaultAsync(usuario => usuario.Id == id, cancellationToken);

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) =>
            appDbContext.Usuarios.AnyAsync(usuario => usuario.Email == email, cancellationToken);

        public Task<bool> CpfCnpjExistsAsync(string cpfCnpjDigits, CancellationToken cancellationToken) =>
            appDbContext.Usuarios.AnyAsync(usuario => usuario.CpfCnpj == cpfCnpjDigits, cancellationToken);

        public Task<Usuario?> FindByIdAsync(int id, CancellationToken cancellationToken) =>
            appDbContext.Usuarios.FirstOrDefaultAsync(usuario => usuario.Id == id, cancellationToken);

        public Task AddAsync(Usuario usuario, CancellationToken cancellationToken) =>
            appDbContext.Usuarios.AddAsync(usuario, cancellationToken).AsTask();
    }
}
