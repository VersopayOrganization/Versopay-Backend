using VersopayBackend.Dtos;
using VersopayBackend.Repositories;
using VersopayBackend.Utils;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Services
{
    public sealed class PedidosService(
        IPedidoRepository pedidoRepository,
        IUsuarioRepository usuarioRepository) : IPedidosService
    {
        public async Task<PedidoResponseDto> CreateAsync(PedidoCreateDto pedidoCreateDto, CancellationToken cancellationToken)
        {
            // valida vendedor
            var vendedor = await usuarioRepository.GetByIdNoTrackingAsync(pedidoCreateDto.VendedorId, cancellationToken);
            if (vendedor is null)
                throw new ArgumentException("VendedorId inválido.");

            // parse método
            if (!Enum.TryParse<MetodoPagamento>(pedidoCreateDto.MetodoPagamento, true, out var metodo))
                throw new ArgumentException("MetodoPagamento inválido. Use: Pix, Boleto, Cartao.");

            var pedido = new Pedido
            {
                VendedorId = pedidoCreateDto.VendedorId,
                Valor = pedidoCreateDto.Valor,
                MetodoPagamento = metodo,
                Produto = string.IsNullOrWhiteSpace(pedidoCreateDto.Produto) ? null : pedidoCreateDto.Produto.Trim(),
                Status = StatusPedido.Pendente,
                Criacao = DateTime.UtcNow
            };

            await pedidoRepository.AddAsync(pedido, cancellationToken);
            await pedidoRepository.SaveChangesAsync(cancellationToken);

            return new PedidoResponseDto
            {
                Id = pedido.Id,
                Criacao = pedido.Criacao,
                CriacaoBr = TimeUtils.ToBrazilOffset(pedido.Criacao),
                DataPagamento = pedido.DataPagamento,
                MetodoPagamento = pedido.MetodoPagamento.ToString(),
                Valor = pedido.Valor,
                VendedorId = pedido.VendedorId,
                VendedorNome = vendedor.Nome,
                Produto = pedido.Produto,
                Status = pedido.Status
            };
        }

        public async Task<IEnumerable<PedidoResponseDto>> GetAllAsync(
            string? status, int? vendedorId, string? metodo,
            DateTime? dataDeUtc, DateTime? dataAteUtc, int page, int pageSize,
            CancellationToken cancellationToken)
        {
            StatusPedido? st = null;
            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<StatusPedido>(status, true, out var sParsed)) st = sParsed;

            MetodoPagamento? mp = null;
            if (!string.IsNullOrWhiteSpace(metodo) &&
                Enum.TryParse<MetodoPagamento>(metodo, true, out var mParsed)) mp = mParsed;

            var list = await pedidoRepository.GetAllAsync(st, vendedorId, mp, dataDeUtc, dataAteUtc, page, pageSize, cancellationToken);
            return list.Select(pedidoResponseDto => new PedidoResponseDto
            {
                Id = pedidoResponseDto.Id,
                Criacao = pedidoResponseDto.Criacao,
                CriacaoBr = TimeUtils.ToBrazilOffset(pedidoResponseDto.Criacao),
                DataPagamento = pedidoResponseDto.DataPagamento,
                MetodoPagamento = pedidoResponseDto.MetodoPagamento.ToString(),
                Valor = pedidoResponseDto.Valor,
                VendedorId = pedidoResponseDto.VendedorId,
                VendedorNome = pedidoResponseDto.Vendedor?.Nome,
                Produto = pedidoResponseDto.Produto,
                Status = pedidoResponseDto.Status
            });
        }

        public async Task<PedidoResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            var pedidoResponseDto = await pedidoRepository.GetByIdNoTrackingAsync(id, cancellationToken);
            if (pedidoResponseDto is null) return null;

            return new PedidoResponseDto
            {
                Id = pedidoResponseDto.Id,
                Criacao = pedidoResponseDto.Criacao,
                CriacaoBr = TimeUtils.ToBrazilOffset(pedidoResponseDto.Criacao),
                DataPagamento = pedidoResponseDto.DataPagamento,
                MetodoPagamento = pedidoResponseDto.MetodoPagamento.ToString(),
                Valor = pedidoResponseDto.Valor,
                VendedorId = pedidoResponseDto.VendedorId,
                VendedorNome = pedidoResponseDto.Vendedor?.Nome,
                Produto = pedidoResponseDto.Produto,
                Status = pedidoResponseDto.Status
            };
        }

        public async Task<bool> UpdateStatusAsync(int id, PedidoStatusUpdateDto pedidoStatusUpdateDto, CancellationToken cancellationToken)
        {
            var pedido = await pedidoRepository.FindByIdAsync(id, cancellationToken);
            if (pedido is null) return false;

            pedido.Status = pedidoStatusUpdateDto.Status;
            if (pedidoStatusUpdateDto.Status == StatusPedido.Pago && pedido.DataPagamento is null)
                pedido.DataPagamento = DateTime.UtcNow;

            await pedidoRepository.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> MarcarComoPagoAsync(int id, CancellationToken cancellationToken)
        {
            var pedido = await pedidoRepository.FindByIdAsync(id, cancellationToken);
            if (pedido is null) return false;

            pedido.Status = StatusPedido.Pago;
            pedido.DataPagamento = pedido.DataPagamento ?? DateTime.UtcNow;

            await pedidoRepository.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
