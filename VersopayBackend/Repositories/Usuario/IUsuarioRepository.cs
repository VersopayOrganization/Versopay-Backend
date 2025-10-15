using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public interface IUsuarioRepository
    {
        // Auth / tokens
        Task<RefreshToken?> GetRefreshWithUserByHashAsync(string tokenHash, CancellationToken cancellationToken);
        Task AddRefreshAsync(RefreshToken token, CancellationToken cancellationToken);

        // Persistência
        Task SaveChangesAsync(CancellationToken cancellationToken);
        Task AddAsync(Usuario usuario, CancellationToken cancellationToken);

        // Leitura
        Task<List<Usuario>> GetAllNoTrackingAsync(CancellationToken cancellationToken);
        Task<Usuario?> GetByIdNoTrackingAsync(int id, CancellationToken cancellationToken);
        Task<Usuario?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<Usuario?> FindByIdAsync(int id, CancellationToken cancellationToken);
        Task<Usuario?> GetByEmailAsync(string email, CancellationToken cancellationToken);

        // Existência / unicidade
        Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);
        Task<bool> CpfExistsAsync(string cpfDigits, CancellationToken cancellationToken);
        Task<bool> CnpjExistsAsync(string cnpjDigits, CancellationToken cancellationToken);
    }
}
