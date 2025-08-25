using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public sealed class AntecipacaoRepository(AppDbContext db) : IAntecipacaoRepository
    {
        public Task AddAsync(Antecipacao a, CancellationToken ct)
            => db.Antecipacoes.AddAsync(a, ct).AsTask();

        public Task<Antecipacao?> FindByIdAsync(int id, CancellationToken ct)
            => db.Antecipacoes.Include(x => x.Empresa).FirstOrDefaultAsync(x => x.Id == id, ct);

        public Task<Antecipacao?> GetByIdNoTrackingAsync(int id, CancellationToken ct)
            => db.Antecipacoes.AsNoTracking().Include(x => x.Empresa).FirstOrDefaultAsync(x => x.Id == id, ct);

        public async Task<List<Antecipacao>> GetAllAsync(
            int? empresaId,
            StatusAntecipacao? status,
            DateTime? deUtc,
            DateTime? ateUtc,
            int page,
            int pageSize,
            CancellationToken ct)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 200) pageSize = 20;

            var q = db.Antecipacoes.AsNoTracking().Include(x => x.Empresa).AsQueryable();
            if (empresaId.HasValue) q = q.Where(x => x.EmpresaId == empresaId.Value);
            if (status.HasValue) q = q.Where(x => x.Status == status.Value);
            if (deUtc.HasValue) q = q.Where(x => x.DataSolicitacao >= deUtc.Value);
            if (ateUtc.HasValue) q = q.Where(x => x.DataSolicitacao < ateUtc.Value);

            return await q.OrderByDescending(x => x.Id)
                          .Skip((page - 1) * pageSize)
                          .Take(pageSize)
                          .ToListAsync(ct);
        }

        public Task<bool> UsuarioExistsAsync(int usuarioId, CancellationToken ct)
            => db.Usuarios.AnyAsync(u => u.Id == usuarioId, ct);

        public Task SaveChangesAsync(CancellationToken ct)
            => db.SaveChangesAsync(ct);
    }
}
