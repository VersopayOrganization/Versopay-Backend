using VersopayBackend.Dtos;

namespace VersopayBackend.Services
{
    public interface IPedidosService
    {
        Task<PedidoResponseDto> CreateAsync(PedidoCreateDto pedidoCreateDto, CancellationToken cancellationToken);
        Task<PedidosTotalResponseDto> GetAllAsync(
            string? status, int? vendedorId, string? metodo,
            DateTime? dataDeUtc, DateTime? dataAteUtc, int page, int pageSize,
            CancellationToken cancellationToken);
        Task<PedidoResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<bool> UpdateStatusAsync(int id, PedidoStatusUpdateDto pedidoStatusUpdatedto, CancellationToken cancellationToken);
        Task<bool> MarcarComoPagoAsync(int id, CancellationToken cancellationToken);
    }
}
