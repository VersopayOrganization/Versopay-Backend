using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public sealed class FaturamentoRepository(AppDbContext appDbContext) : IFaturamentoRepository
    {
        public Task AddAsync(Faturamento faturamento, CancellationToken cancellationToken)
            => appDbContext.Faturamentos.AddAsync(faturamento, cancellationToken).AsTask();

        public Task<Faturamento?> GetByIdAsync(int id, CancellationToken cancellationToken)
            => appDbContext.Faturamentos.AsNoTracking().FirstOrDefaultAsync(faturamento => faturamento.Id == id, cancellationToken);

        public async Task<List<Faturamento>> ListByCpfCnpjAsync(string cpfCnpj, DateTime? dataInicio, DateTime? dataFim, CancellationToken cancellationToken)
        {
            var faturamentos = appDbContext.Faturamentos.AsNoTracking().Where(faturamento => faturamento.CpfCnpj == cpfCnpj);
            if (dataInicio.HasValue) faturamentos = faturamentos.Where(faturamento => faturamento.DataFim >= dataInicio.Value);
            if (dataFim.HasValue) faturamentos = faturamentos.Where(faturamento => faturamento.DataInicio <= dataFim.Value);
            return await faturamentos.OrderByDescending(faturamento => faturamento.DataInicio).ToListAsync(cancellationToken);
        }

        public Task<Faturamento?> GetLatestByCpfCnpjAsync(string cpfCnpj, CancellationToken cancellationToken)
            => appDbContext.Faturamentos.AsNoTracking()
                  .Where(faturamento => faturamento.CpfCnpj == cpfCnpj)
                  .OrderByDescending(faturamento => faturamento.AtualizadoEmUtc)
                  .FirstOrDefaultAsync(cancellationToken);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => appDbContext.SaveChangesAsync(cancellationToken);
    }
}
