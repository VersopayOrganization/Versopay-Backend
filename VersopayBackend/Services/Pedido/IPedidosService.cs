using VersopayBackend.Dtos;
using VersopayLibrary.Enums;

namespace VersopayBackend.Services
{
    public interface IPedidosService
    {
        Task<PedidoDto> CreateAsync(PedidoCreateDto pedidoCreateDto, CancellationToken cancellationToken);
        Task<PedidosResponseDto> GetAllAsync(
            StatusPedido? StatusPedido, int? vendedorId, MetodoPagamento? metodo,
            DateTime? dataDe, DateTime? dataAte, int page, int pageSize,
            CancellationToken cancellationToken);
        Task<PedidoDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<bool> UpdateStatusAsync(int id, PedidoStatusUpdateDto pedidoStatusUpdatedto, CancellationToken cancellationToken);
        Task<bool> MarcarComoPagoAsync(int id, CancellationToken cancellationToken);
    }
}
