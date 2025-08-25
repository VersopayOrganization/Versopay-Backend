using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public interface IAntecipacaoRepository
    {
        Task AddAsync(Antecipacao a, CancellationToken ct);
        Task<Antecipacao?> FindByIdAsync(int id, CancellationToken ct);
        Task<Antecipacao?> GetByIdNoTrackingAsync(int id, CancellationToken ct);
        Task<List<Antecipacao>> GetAllAsync(
            int? empresaId,
            StatusAntecipacao? status,
            DateTime? deUtc,
            DateTime? ateUtc,
            int page,
            int pageSize,
            CancellationToken ct);

        Task<bool> UsuarioExistsAsync(int usuarioId, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}
