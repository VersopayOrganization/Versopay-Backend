using VersopayBackend.Dtos;
using VersopayLibrary.Enums;

namespace VersopayBackend.Services
{
    public interface ITransferenciasService
    {
        Task<TransferenciaResponseDto> CreateAsync(TransferenciaCreateDto transfereciaCreateDto, CancellationToken cancellationToken);
        Task<IEnumerable<TransferenciaResponseDto>> GetAllAsync(
            int? solicitanteId, StatusTransferencia? status, DateTime? dataInicio, DateTime? dataFim,
            int page, int pageSize, CancellationToken cancellationToken);
        Task<TransferenciaResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<TransferenciaResponseDto?> AdminUpdateAsync(int id, TransferenciaAdminUpdateDto transfereciaAdminUpdateDto, CancellationToken cancellationToken);
        Task<bool> CancelarAsync(int id, CancellationToken cancellationToken);
        Task<bool> ConcluirAsync(int id, decimal? taxa, decimal? valorFinal, CancellationToken cancellationToken);
    }
}
