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
            => appDbContext.Faturamentos.AsNoTracking()
                 .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        public async Task<List<Faturamento>> ListByCpfCnpjAsync(string cpfCnpjDigits, DateTime? dataInicio, DateTime? dataFim, CancellationToken ct)
        {
            var q = appDbContext.Faturamentos.AsNoTracking();

            if (cpfCnpjDigits?.Length == 11)
                q = q.Where(f => f.Cpf == cpfCnpjDigits);
            else if (cpfCnpjDigits?.Length == 14)
                q = q.Where(f => f.Cnpj == cpfCnpjDigits);
            else
                q = q.Where(f => false); // inválido => retorna vazio

            if (dataInicio.HasValue) q = q.Where(f => f.DataFim >= dataInicio.Value);
            if (dataFim.HasValue) q = q.Where(f => f.DataInicio <= dataFim.Value);

            return await q.OrderByDescending(f => f.DataInicio).ToListAsync(ct);
        }

        public Task<Faturamento?> GetLatestByCpfCnpjAsync(string cpfCnpjDigits, CancellationToken ct)
        {
            var q = appDbContext.Faturamentos.AsNoTracking();

            if (cpfCnpjDigits?.Length == 11)
                q = q.Where(f => f.Cpf == cpfCnpjDigits);
            else if (cpfCnpjDigits?.Length == 14)
                q = q.Where(f => f.Cnpj == cpfCnpjDigits);
            else
                q = q.Where(f => false);

            return q.OrderByDescending(f => f.AtualizadoEmUtc).FirstOrDefaultAsync(ct);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => appDbContext.SaveChangesAsync(cancellationToken);
    }
}
