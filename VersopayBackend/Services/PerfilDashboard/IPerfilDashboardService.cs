using VersopayBackend.Dtos;

namespace VersopayBackend.Services.PerfilDashboard
{
    public interface IPerfilDashboardService
    {
        Task<PerfilResponseDto> GetPerfilAsync(int usuarioId, CancellationToken cancellationToken);
        Task<DashboardResponseDto> GetDashboardAsync(int usuarioId, DashboardQueryDto dashboardQueryDto, CancellationToken cancellationToken);
    }
}
