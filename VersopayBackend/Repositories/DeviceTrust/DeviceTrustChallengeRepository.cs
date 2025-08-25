// VersopayBackend/Repositories/DeviceTrustChallengeRepository.cs
using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public sealed class DeviceTrustChallengeRepository(AppDbContext db) : IDeviceTrustChallengeRepository
    {
        public Task<DeviceTrustChallenge?> GetAsync(Guid id, CancellationToken ct) =>
            db.DeviceTrustChallenges.Include(x => x.Usuario)
              .FirstOrDefaultAsync(x => x.Id == id, ct);

        public Task AddAsync(DeviceTrustChallenge entity, CancellationToken ct) =>
            db.DeviceTrustChallenges.AddAsync(entity, ct).AsTask();

        public async Task InvalidateUserOpenAsync(int usuarioId, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            await db.DeviceTrustChallenges
                    .Where(x => x.UsuarioId == usuarioId && !x.Used && x.ExpiresAtUtc > now)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Used, true), ct);
        }

        public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    }
}
