using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories.NovaSenha
{
    public class NovaSenhaRepository(AppDbContext appDbContext) : INovaSenhaRepository
    {
        public Task AddAsync(NovaSenhaResetToken novaSenhaResetToken, CancellationToken cancellationToken) =>
        appDbContext.NovaSenhaResetTokens.AddAsync(novaSenhaResetToken, cancellationToken).AsTask();

        public Task<NovaSenhaResetToken?> GetByHashWithUserAsync(string tokenHash, CancellationToken cancellationToken) =>
            appDbContext.NovaSenhaResetTokens.Include(x => x.Usuario)
                                  .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        public async Task InvalidateUserTokensAsync(int usuarioId, CancellationToken cancellationToken)
        {
            await appDbContext.NovaSenhaResetTokens
                .Where(x => x.UsuarioId == usuarioId && x.DataTokenUsado == null && x.DataExpiracao > DateTime.UtcNow)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.DataTokenUsado, DateTime.UtcNow), cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => appDbContext.SaveChangesAsync(cancellationToken);
    }
}