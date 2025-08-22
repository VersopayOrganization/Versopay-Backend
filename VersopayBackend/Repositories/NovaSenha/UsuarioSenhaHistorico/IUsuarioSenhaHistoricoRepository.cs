using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public interface IUsuarioSenhaHistoricoRepository
    {
        Task AddAsync(UsuarioSenhaHistorico historico, CancellationToken cancellationToken);
        Task<List<UsuarioSenhaHistorico>> GetByUsuarioAsync(int usuarioId, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}