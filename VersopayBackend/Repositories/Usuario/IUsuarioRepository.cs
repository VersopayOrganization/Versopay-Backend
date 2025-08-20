using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public interface IUsuarioRepository
    {
        // === já existentes (Auth / Refresh) ===
        Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct);
        Task<RefreshToken?> GetRefreshWithUserByHashAsync(string tokenHash, CancellationToken ct);
        Task AddRefreshAsync(RefreshToken token, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);

        // === novos (consultas ficam no repo) ===
        Task<List<Usuario>> GetAllNoTrackingAsync(CancellationToken ct);
        Task<Usuario?> GetByIdNoTrackingAsync(int id, CancellationToken ct);

        Task<bool> EmailExistsAsync(string email, CancellationToken ct);
        Task<bool> CpfCnpjExistsAsync(string cpfCnpjDigits, CancellationToken ct);
        Task<Usuario?> FindByIdAsync(int id, CancellationToken ct);
        Task AddAsync(Usuario u, CancellationToken ct);
    }
}
