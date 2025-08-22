using VersopayBackend.Dtos;

namespace VersopayBackend.Services.KycKyb
{
    public interface IKycKybService
    {
        Task<KycKybResponseDto> CriarAsync(KycKybCreateDto kycKybCreateDto, CancellationToken cancellationToken);
        Task<IEnumerable<KycKybResponseDto>> PegarTodosAsync(int? usuarioId, string? status, int page, int pageSize, CancellationToken cancellationToken);
        Task<KycKybResponseDto?> PegarPeloIdAsync(int id, CancellationToken cancellationToken);
        Task<bool> AtualizarStatusAsync(int id, KycKybStatusUpdateDto kycKybStatusUpdateDto, CancellationToken cancellationToken);
        Task<bool> AprovarAsync(int id, CancellationToken cancellationToken);
        Task<bool> ReprovarAsync(int id, CancellationToken cancellationToken);
    }
}
