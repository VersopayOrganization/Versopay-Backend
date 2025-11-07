using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{

    public sealed class TransferenciaRepository : ITransferenciaRepository
    {
        private readonly AppDbContext _db;
        public TransferenciaRepository(AppDbContext db) => _db = db;

        public IQueryable<Transferencia> QueryNoTracking() => _db.Transferencias.AsNoTracking();

        public Task AddAsync(Transferencia entity, CancellationToken ct) =>
            _db.Transferencias.AddAsync(entity, ct).AsTask();

        public Task<Transferencia?> FindByIdAsync(int id, CancellationToken ct) =>
            _db.Transferencias.FirstOrDefaultAsync(x => x.Id == id, ct);

        public Task<Transferencia?> GetByIdNoTrackingAsync(int id, CancellationToken ct) =>
            _db.Transferencias.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

        public async Task<List<Transferencia>> GetAllAsync(
            int? solicitanteId,
            StatusTransferencia? status,
            DateTime? dataInicio,
            DateTime? dataFim,
            int page,
            int pageSize,
            CancellationToken ct)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 200) pageSize = 20;

            var q = _db.Transferencias.AsNoTracking().AsQueryable();

            if (solicitanteId.HasValue) q = q.Where(x => x.SolicitanteId == solicitanteId.Value);
            if (status.HasValue) q = q.Where(x => x.Status == status.Value);
            if (dataInicio.HasValue) q = q.Where(x => x.DataSolicitacao >= dataInicio.Value);
            if (dataFim.HasValue) q = q.Where(x => x.DataSolicitacao < dataFim.Value);

            return await q.OrderByDescending(x => x.DataSolicitacao)
                          .Skip((page - 1) * pageSize)
                          .Take(pageSize)
                          .ToListAsync(ct);
        }

        public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
    }
}
