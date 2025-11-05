using VersopayBackend.Dtos;

namespace VersopayBackend.Services
{
    public interface IPedidosService
    {
        Task<PedidoDto> CreateAsync(PedidoCreateDto dto, CancellationToken ct);
        Task<PedidosResponseDto> GetAllAsync(string? status, int? vendedorId, string? metodo, DateTime? dataInicio, DateTime? dataFim, int page, int pageSize, CancellationToken ct);
        Task<PedidoDto?> GetByIdAsync(int id, CancellationToken ct);
        Task<bool> UpdateStatusAsync(int id, PedidoStatusUpdateDto dto, CancellationToken ct);
        Task<bool> MarcarComoPagoAsync(int id, CancellationToken ct);
    }
}
