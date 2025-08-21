using VersopayBackend.Dtos;

namespace VersopayBackend.Services.KycKyb
{
    public interface IKycKybService
    {
        Task<KycKybResponseDto> CreateAsync(KycKybCreateDto dto, CancellationToken ct);
        Task<IEnumerable<KycKybResponseDto>> GetAllAsync(int? usuarioId, string? status, int page, int pageSize, CancellationToken ct);
        Task<KycKybResponseDto?> GetByIdAsync(int id, CancellationToken ct);
        Task<bool> UpdateStatusAsync(int id, KycKybStatusUpdateDto dto, CancellationToken ct);
        Task<bool> AprovarAsync(int id, CancellationToken ct);
        Task<bool> ReprovarAsync(int id, CancellationToken ct);
    }
}
