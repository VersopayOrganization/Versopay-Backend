using VersopayBackend.Dtos;
using VersopayLibrary.Enums;

namespace VersopayBackend.Services
{
    public interface ITransferenciasService
    {
        Task<TransferenciaResponseDto> CreateAsync(TransferenciaCreateDto body, CancellationToken ct);
        Task<IEnumerable<TransferenciaResponseDto>> GetAllAsync(int? solicitanteId, StatusTransferencia? status, DateTime? inicio, DateTime? fim, int page, int pageSize, CancellationToken ct);
        Task<TransferenciaResponseDto?> GetByIdAsync(int id, CancellationToken ct);
        Task<TransferenciaResponseDto?> AdminUpdateAsync(int id, TransferenciaAdminUpdateDto body, CancellationToken ct);
        Task<bool> CancelarAsync(int id, CancellationToken ct);
        Task<bool> ConcluirAsync(int id, decimal? taxa, decimal? valorFinal, CancellationToken ct);
    }
}
