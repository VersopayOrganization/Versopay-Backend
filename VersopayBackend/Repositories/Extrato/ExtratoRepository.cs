using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public sealed class ExtratoRepository : IExtratoRepository
    {
        private readonly AppDbContext _db;
        public ExtratoRepository(AppDbContext db) => _db = db;

        public Task<Extrato?> GetByClienteIdAsync(int clienteId, CancellationToken ct) =>
            _db.Extratos.FirstOrDefaultAsync(e => e.ClienteId == clienteId, ct);

        public Task<Extrato?> GetByClienteIdNoTrackingAsync(int clienteId, CancellationToken ct) =>
            _db.Extratos.AsNoTracking().FirstOrDefaultAsync(e => e.ClienteId == clienteId, ct);

        public Task AddAsync(Extrato entity, CancellationToken ct) =>
            _db.Extratos.AddAsync(entity, ct).AsTask();

        public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
    }
}
