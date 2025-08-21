using VersopayLibrary.Models;

namespace VersopayBackend.Repositories.NovaSenha
{
    public interface INovaSenhaRepository
    {
        Task AddAsync(NovaSenhaResetToken passwordResetToken, CancellationToken cancellationToken);
        Task<NovaSenhaResetToken?> GetByHashWithUserAsync(string tokenHash, CancellationToken cancellationToken);
        Task InvalidateUserTokensAsync(int usuarioId, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}