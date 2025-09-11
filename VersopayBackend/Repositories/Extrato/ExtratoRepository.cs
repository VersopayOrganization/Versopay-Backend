using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public sealed class ExtratoRepository(AppDbContext appDbContext) : IExtratoRepository
    {
        public Task<Extrato?> GetByClienteIdAsync(int clienteId, CancellationToken cancellationToken) =>
            appDbContext.Extratos.FirstOrDefaultAsync(extrato => extrato.ClienteId == clienteId, cancellationToken);

        public Task<Extrato?> GetByClienteIdNoTrackingAsync(int clienteId, CancellationToken cancellationToken) =>
            appDbContext.Extratos.AsNoTracking().FirstOrDefaultAsync(extrato => extrato.ClienteId == clienteId, cancellationToken);

        public Task AddAsync(Extrato extrato, CancellationToken cancellationToken) =>
            appDbContext.Extratos.AddAsync(extrato, cancellationToken).AsTask();

        public Task SaveChangesAsync(CancellationToken cancellationToken) => appDbContext.SaveChangesAsync(cancellationToken);
    }
}
