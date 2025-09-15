using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public interface IMovimentacaoRepository
    {
        IQueryable<MovimentacaoFinanceira> QueryNoTracking();
        Task AddAsync(MovimentacaoFinanceira movimentacaoFinanceira, CancellationToken cancellationToken);
        Task<MovimentacaoFinanceira?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
