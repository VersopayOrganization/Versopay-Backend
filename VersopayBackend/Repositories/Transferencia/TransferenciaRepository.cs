using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public sealed class TransferenciaRepository(AppDbContext appDbContext) : ITransferenciaRepository
    {
        public IQueryable<Transferencia> QueryNoTracking() =>
            appDbContext.Transferencias.AsNoTracking();

        public Task AddAsync(Transferencia transferencia, CancellationToken cancellationToken) =>
            appDbContext.Transferencias.AddAsync(transferencia, cancellationToken).AsTask();

        public Task<Transferencia?> FindByIdAsync(int id, CancellationToken cancellationToken) =>
            appDbContext.Transferencias.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        public Task<Transferencia?> GetByIdNoTrackingAsync(int id, CancellationToken cancellationToken) =>
            appDbContext.Transferencias.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        public async Task<List<Transferencia>> GetAllAsync(
            int? solicitanteId,
            StatusTransferencia? status,
            DateTime? dataInicio,
            DateTime? dataFim,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 200) pageSize = 20;

            var query = appDbContext.Transferencias.AsNoTracking().AsQueryable();

            if (solicitanteId.HasValue)
                query = query.Where(x => x.SolicitanteId == solicitanteId.Value);

            if (status.HasValue)
                query = query.Where(x => x.Status == status.Value);

            if (dataInicio.HasValue)
                query = query.Where(x => x.DataSolicitacao >= dataInicio.Value);

            if (dataFim.HasValue)
                query = query.Where(x => x.DataSolicitacao <= dataFim.Value);

            return await query.OrderByDescending(x => x.DataSolicitacao)
                          .Skip((page - 1) * pageSize)
                          .Take(pageSize)
                          .ToListAsync(cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => appDbContext.SaveChangesAsync(cancellationToken);
    }
}
