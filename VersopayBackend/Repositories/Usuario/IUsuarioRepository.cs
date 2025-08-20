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
        Task<Usuario?> GetByEmailAsync(string email, CancellationToken cancellationToken);
        Task<RefreshToken?> GetRefreshWithUserByHashAsync(string tokenHash, CancellationToken cancellationToken);
        Task AddRefreshAsync(RefreshToken token, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);

        // === novos (consultas ficam no repo) ===
        Task<List<Usuario>> GetAllNoTrackingAsync(CancellationToken cancellationToken);
        Task<Usuario?> GetByIdNoTrackingAsync(int id, CancellationToken cancellationToken);

        Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);
        Task<bool> CpfCnpjExistsAsync(string cpfCnpjDigits, CancellationToken cancellationToken);
        Task<Usuario?> FindByIdAsync(int id, CancellationToken cancellationToken);
        Task AddAsync(Usuario usuario, CancellationToken cancellationToken);
    }
}
