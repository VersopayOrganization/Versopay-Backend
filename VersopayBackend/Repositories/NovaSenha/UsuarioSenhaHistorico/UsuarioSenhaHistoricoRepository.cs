using Microsoft.EntityFrameworkCore;
using VersopayDatabase.Data;
using VersopayLibrary.Models;

namespace VersopayBackend.Repositories
{
    public class UsuarioSenhaHistoricoRepository(AppDbContext appDbContext) : IUsuarioSenhaHistoricoRepository
    {

        public async Task AddAsync(UsuarioSenhaHistorico historico, CancellationToken cancellationToken)
        {
            await appDbContext.UsuarioSenhasHistorico.AddAsync(historico, cancellationToken);
        }

        public async Task<List<UsuarioSenhaHistorico>> GetByUsuarioAsync(int usuarioId, CancellationToken cancellationToken)
        {
            return await appDbContext.UsuarioSenhasHistorico
                .Where(x => x.UsuarioId == usuarioId)
                .OrderByDescending(x => x.DataCriacao)
                .ToListAsync(cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            await appDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
