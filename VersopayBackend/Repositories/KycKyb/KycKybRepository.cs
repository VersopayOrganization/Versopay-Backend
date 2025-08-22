using Microsoft.EntityFrameworkCore;
using VersopayBackend.Repositories;
using VersopayDatabase.Data;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public sealed class KycKybRepository(AppDbContext appDbContext) : IKycKybRepository
    {
        public Task AdicionarAsync(KycKyb item, CancellationToken cancellationToken) =>
            appDbContext.KycKybs.AddAsync(item, cancellationToken).AsTask();

        public Task<KycKyb?> AcharPeloIdAsync(int id, CancellationToken cancellationToken) =>
            appDbContext.KycKybs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        public Task<KycKyb?> PegarPeloIdNoTrackingAsync(int id, CancellationToken cancellationToken) =>
            appDbContext.KycKybs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        public async Task<List<KycKyb>> PegarTodosAsync(
            int? usuarioId,
            StatusKycKyb? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 200) pageSize = 20;

            var query = appDbContext.KycKybs.AsNoTracking().AsQueryable();
            if (usuarioId.HasValue) query = query.Where(x => x.UsuarioId == usuarioId.Value);
            if (status.HasValue) query = query.Where(x => x.Status == status.Value);

            return await query.OrderByDescending(x => x.Id)
                          .Skip((page - 1) * pageSize)
                          .Take(pageSize)
                          .ToListAsync(cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => appDbContext.SaveChangesAsync(cancellationToken);
    }
}
