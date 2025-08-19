using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public interface IUsuarioRepository
    {
        // === JÁ EXISTENTES (Auth / Refresh) ===
        Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct);
        Task<RefreshToken?> GetRefreshWithUserByHashAsync(string tokenHash, CancellationToken ct);
        Task AddRefreshAsync(RefreshToken token, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);

        // === NOVOS (CRUD / Consultas para UsuariosService) ===
        IQueryable<Usuario> QueryNoTracking();
        Task<bool> EmailExistsAsync(string email, CancellationToken ct);
        Task<bool> CpfCnpjExistsAsync(string cpfCnpjDigits, CancellationToken ct);
        Task<Usuario?> FindByIdAsync(int id, CancellationToken ct);
        Task AddAsync(Usuario u, CancellationToken ct);
    }
}
