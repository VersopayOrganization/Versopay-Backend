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
        public async Task<PedidoDto> CreateAsync(PedidoCreateDto pedidoCreateDto, CancellationToken cancellationToken)
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

            return new PedidoDto
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

        public async Task<PedidosResponseDto> GetAllAsync(
            string? status, int? vendedorId, string? metodo,
            DateTime? dataInicio, DateTime? dataFim, int page, int pageSize,
            CancellationToken cancellationToken)
        {
            StatusPedido? statusPedido = null;
            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<StatusPedido>(status, true, out var sParsed)) statusPedido = sParsed;

            MetodoPagamento? metodoPagamento = null;
            if (!string.IsNullOrWhiteSpace(metodo) &&
                Enum.TryParse<MetodoPagamento>(metodo, true, out var mParsed)) metodoPagamento = mParsed;

            var count = await pedidoRepository.GetCountAllAsync(statusPedido, vendedorId, metodoPagamento, dataInicio, dataFim, cancellationToken);
            var list = await pedidoRepository.GetAllAsync(statusPedido, vendedorId, metodoPagamento, dataInicio, dataFim, page, pageSize, cancellationToken);
            return new PedidosResponseDto
            {
                Pedidos = list.Select(pedidoResponseDto => new PedidoDto
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
                }),
                TotalRegistros = count
            };
        }

        public async Task<PedidoDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            var pedidoResponseDto = await pedidoRepository.GetByIdNoTrackingAsync(id, cancellationToken);
            if (pedidoResponseDto is null) return null;

            return new PedidoDto
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
            if (pedidoStatusUpdateDto.Status == StatusPedido.Aprovado && pedido.DataPagamento is null)
                pedido.DataPagamento = DateTime.UtcNow;

            await pedidoRepository.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> MarcarComoPagoAsync(int id, CancellationToken cancellationToken)
        {
            var pedido = await pedidoRepository.FindByIdAsync(id, cancellationToken);
            if (pedido is null) return false;

            pedido.Status = StatusPedido.Aprovado;
            pedido.DataPagamento = pedido.DataPagamento ?? DateTime.UtcNow;

            await pedidoRepository.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
