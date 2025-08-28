using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public interface IAntecipacaoRepository
    {
        Task AddAsync(Antecipacao antecipacao, CancellationToken cancellationToken);
        Task<Antecipacao?> FindByIdAsync(int id, CancellationToken cancellationToken);
        Task<Antecipacao?> GetByIdNoTrackingAsync(int id, CancellationToken cancellationToken);
        Task<List<Antecipacao>> GetAllAsync(
            int? empresaId,
            StatusAntecipacao? status,
            DateTime? dataInicio,
            DateTime? dataFim,
            int page,
            int pageSize,
            CancellationToken cancellationToken);

        Task<bool> UsuarioExistsAsync(int usuarioId, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
