using VersopayBackend.Dtos;
using VersopayLibrary.Enums;

namespace VersopayBackend.Services
{
    public interface IAntecipacoesService
    {
        Task<AntecipacaoResponseDto> CreateAsync(AntecipacaoCreateDto antecipacaoCreateDto, CancellationToken cancellationToken);
        Task<IEnumerable<AntecipacaoResponseDto>> GetAllAsync(int? empresaId, string? status, DateTime? dataInicio, DateTime? dataFim, int page, int pageSize, CancellationToken cancellationToken);
        Task<AntecipacaoResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<bool> UpdateStatusAsync(int id, AntecipacaoStatusUpdateDto antecipacaoStatusUpdateDto, CancellationToken cancellationToken);

        // atalhos
        Task<bool> CancelarAsync(int id, CancellationToken cancellationToken) =>
            UpdateStatusAsync(id, new AntecipacaoStatusUpdateDto { Status = StatusAntecipacao.Cancelado }, cancellationToken);

        Task<bool> ConcluirAsync(int id, CancellationToken cancellationToken) =>
            UpdateStatusAsync(id, new AntecipacaoStatusUpdateDto { Status = StatusAntecipacao.Concluido }, cancellationToken);
    }
}
