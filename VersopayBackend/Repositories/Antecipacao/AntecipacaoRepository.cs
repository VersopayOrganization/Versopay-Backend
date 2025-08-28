using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public sealed class AntecipacaoRepository(AppDbContext appDbContext) : IAntecipacaoRepository
    {
        public Task AddAsync(Antecipacao antecipacao, CancellationToken cancellationToken)
            => appDbContext.Antecipacoes.AddAsync(antecipacao, cancellationToken).AsTask();

        public Task<Antecipacao?> FindByIdAsync(int id, CancellationToken cancellationToken)
            => appDbContext.Antecipacoes.Include(x => x.Empresa).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        public Task<Antecipacao?> GetByIdNoTrackingAsync(int id, CancellationToken cancellationToken)
            => appDbContext.Antecipacoes.AsNoTracking().Include(x => x.Empresa).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        public async Task<List<Antecipacao>> GetAllAsync(
            int? empresaId,
            StatusAntecipacao? status,
            DateTime? dataInicio,
            DateTime? dataFim,
            int page,
            int pageSize,
            CancellationToken ct)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 200) pageSize = 20;

            var q = appDbContext.Antecipacoes.AsNoTracking().Include(x => x.Empresa).AsQueryable();
            if (empresaId.HasValue) q = q.Where(x => x.EmpresaId == empresaId.Value);
            if (status.HasValue) q = q.Where(x => x.Status == status.Value);
            if (dataInicio.HasValue) q = q.Where(x => x.DataSolicitacao >= dataInicio.Value);
            if (dataFim.HasValue) q = q.Where(x => x.DataSolicitacao < dataFim.Value);

            return await q.OrderByDescending(x => x.Id)
                          .Skip((page - 1) * pageSize)
                          .Take(pageSize)
                          .ToListAsync(ct);
        }

        public Task<bool> UsuarioExistsAsync(int usuarioId, CancellationToken ct)
            => appDbContext.Usuarios.AnyAsync(u => u.Id == usuarioId, ct);

        public Task SaveChangesAsync(CancellationToken ct)
            => appDbContext.SaveChangesAsync(ct);
    }
}
