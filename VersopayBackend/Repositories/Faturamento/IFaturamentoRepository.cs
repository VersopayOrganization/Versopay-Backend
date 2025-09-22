using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public interface IFaturamentoRepository
    {
        Task AddAsync(Faturamento faturamento, CancellationToken cancellationToken);
        Task<Faturamento?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<List<Faturamento>> ListByCpfCnpjAsync(string cpfCnpj, DateTime? dataInicio, DateTime? dataFim, CancellationToken cancellationToken);
        Task<Faturamento?> GetLatestByCpfCnpjAsync(string cpfCnpj, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
