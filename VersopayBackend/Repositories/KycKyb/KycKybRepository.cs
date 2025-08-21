using Microsoft.EntityFrameworkCore;
using VersopayBackend.Repositories;
using VersopayDatabase.Data;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public sealed class KycKybRepository(AppDbContext db) : IKycKybRepository
    {
        public Task AddAsync(KycKyb item, CancellationToken ct) =>
            db.KycKybs.AddAsync(item, ct).AsTask();

        public Task<KycKyb?> FindByIdAsync(int id, CancellationToken ct) =>
            db.KycKybs.FirstOrDefaultAsync(x => x.Id == id, ct);

        public Task<KycKyb?> GetByIdNoTrackingAsync(int id, CancellationToken ct) =>
            db.KycKybs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

        public async Task<List<KycKyb>> GetAllAsync(
            int? usuarioId,
            StatusKycKyb? status,
            int page,
            int pageSize,
            CancellationToken ct)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 200) pageSize = 20;

            var q = db.KycKybs.AsNoTracking().AsQueryable();
            if (usuarioId.HasValue) q = q.Where(x => x.UsuarioId == usuarioId.Value);
            if (status.HasValue) q = q.Where(x => x.Status == status.Value);

            return await q.OrderByDescending(x => x.Id)
                          .Skip((page - 1) * pageSize)
                          .Take(pageSize)
                          .ToListAsync(ct);
        }

        public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    }
}
