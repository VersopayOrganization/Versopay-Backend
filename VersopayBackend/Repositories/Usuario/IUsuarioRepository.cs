using VersopayBackend.Dtos;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public interface IUsuarioRepository
    {
        Task<Usuario?> GetByEmailAsync(string email, CancellationToken cancellationToken);
        Task<RefreshToken?> GetRefreshWithUserByHashAsync(string tokenHash, CancellationToken cancellationToken);
        Task AddRefreshAsync(RefreshToken token, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
        Task<List<Usuario>> GetAllNoTrackingAsync(CancellationToken cancellationToken);
        Task<Usuario?> GetByIdNoTrackingAsync(int id, CancellationToken cancellationToken);
        Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);
        Task<bool> CpfCnpjExistsAsync(string cpfCnpjDigits, CancellationToken cancellationToken);
        Task<Usuario?> FindByIdAsync(int id, CancellationToken cancellationToken);
        Task AddAsync(Usuario usuario, CancellationToken cancellationToken);
    }
}