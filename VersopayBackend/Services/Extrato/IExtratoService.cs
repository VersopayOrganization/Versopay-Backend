using VersopayBackend.Dtos;

namespace VersopayBackend.Services
{
    public interface IExtratoService
    {
        Task<ExtratoResponseDto> GetByClienteAsync(int clienteId, CancellationToken ct);
        Task<MovimentacaoResponseDto> LancarAsync(MovimentacaoCreateDto body, CancellationToken ct);
        Task<MovimentacaoResponseDto?> ConfirmarAsync(Guid id, CancellationToken ct);
        Task<bool> CancelarAsync(Guid id, CancellationToken ct);
        Task<IEnumerable<MovimentacaoResponseDto>> ListarMovimentacoesAsync(int clienteId, MovimentacaoFiltroDto filtro, CancellationToken ct);
    }
}
