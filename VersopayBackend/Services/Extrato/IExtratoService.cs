using VersopayBackend.Dtos;

namespace VersopayBackend.Services
{
    public interface IExtratoService
    {
        Task<ExtratoResponseDto> GetByClienteAsync(int clienteId, CancellationToken cancellationToken);

        Task<MovimentacaoResponseDto> LancarAsync(MovimentacaoCreateDto movimentacaoCreateDto, CancellationToken cancellationToken);
        Task<MovimentacaoResponseDto?> ConfirmarAsync(Guid id, CancellationToken cancellationToken);
        Task<bool> CancelarAsync(Guid id, CancellationToken cancellationToken);

        Task<IEnumerable<MovimentacaoResponseDto>> ListarMovimentacoesAsync(
            int clienteId, MovimentacaoFiltroDto filtro, CancellationToken cancellationToken);
    }
}
