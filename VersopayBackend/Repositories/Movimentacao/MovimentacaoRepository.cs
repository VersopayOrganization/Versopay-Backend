using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public sealed class MovimentacaoRepository(AppDbContext appDbContext) : IMovimentacaoRepository
    {
        public IQueryable<MovimentacaoFinanceira> QueryNoTracking() =>
            appDbContext.MovimentacoesFinanceiras.AsNoTracking();

        public Task AddAsync(MovimentacaoFinanceira movimentacaoFinanceira, CancellationToken cancellationToken) =>
            appDbContext.MovimentacoesFinanceiras.AddAsync(movimentacaoFinanceira, cancellationToken).AsTask();

        public Task<MovimentacaoFinanceira?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
            appDbContext.MovimentacoesFinanceiras.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => appDbContext.SaveChangesAsync(cancellationToken);
    }
}
