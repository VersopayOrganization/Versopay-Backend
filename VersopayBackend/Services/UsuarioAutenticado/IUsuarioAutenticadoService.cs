using VersopayBackend.Dtos;

namespace VersopayBackend.Services
{
    public interface IUsuarioAutenticadoService
    {
        Task<PerfilResumoDto> GetPerfilAsync(int usuarioId, CancellationToken ct);
        Task<DashboardResumoDto> GetDashboardAsync(int usuarioId, DateTime deUtc, DateTime ateUtc, CancellationToken ct);
    }
}
