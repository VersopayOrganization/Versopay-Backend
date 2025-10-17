using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public interface IFaturamentoRepository
    {
        Task AddAsync(Faturamento faturamento, CancellationToken cancellationToken);
        Task<Faturamento?> GetByIdAsync(int id, CancellationToken cancellationToken);

        // documento pode ser CPF (11) ou CNPJ (14)
        Task<List<Faturamento>> ListByCpfCnpjAsync(string cpfCnpjDigits, DateTime? dataInicio, DateTime? dataFim, CancellationToken cancellationToken);
        Task<Faturamento?> GetLatestByCpfCnpjAsync(string cpfCnpjDigits, CancellationToken cancellationToken);

        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
