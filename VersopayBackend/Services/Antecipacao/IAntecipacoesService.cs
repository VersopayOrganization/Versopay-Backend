using VersopayBackend.Dtos;
using VersopayLibrary.Enums;

namespace VersopayBackend.Services
{
    public interface IAntecipacoesService
    {
        Task<AntecipacaoResponseDto> CreateAsync(AntecipacaoCreateDto dto, CancellationToken ct);
        Task<IEnumerable<AntecipacaoResponseDto>> GetAllAsync(int? empresaId, string? status, DateTime? deUtc, DateTime? ateUtc, int page, int pageSize, CancellationToken ct);
        Task<AntecipacaoResponseDto?> GetByIdAsync(int id, CancellationToken ct);
        Task<bool> UpdateStatusAsync(int id, AntecipacaoStatusUpdateDto dto, CancellationToken ct);

        // atalhos
        Task<bool> CancelarAsync(int id, CancellationToken ct) =>
            UpdateStatusAsync(id, new AntecipacaoStatusUpdateDto { Status = StatusAntecipacao.Cancelado }, ct);

        Task<bool> ConcluirAsync(int id, CancellationToken ct) =>
            UpdateStatusAsync(id, new AntecipacaoStatusUpdateDto { Status = StatusAntecipacao.Concluido }, ct);
    }
}
