using VersopayBackend.Dtos;
using VersopayBackend.Repositories;
using VersopayBackend.Utils;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Services
{
    public sealed class PedidosService : IPedidosService
    {
        private readonly IPedidoRepository _pedidoRepository;
        private readonly IUsuarioRepository _usuarioRepository;

        public PedidosService(IPedidoRepository pedidoRepository, IUsuarioRepository usuarioRepository)
        {
            _pedidoRepository = pedidoRepository;
            _usuarioRepository = usuarioRepository;
        }

        public async Task<PedidoDto> CreateAsync(PedidoCreateDto dto, CancellationToken ct)
        {
            var vendedor = await _usuarioRepository.GetByIdNoTrackingAsync(dto.VendedorId, ct)
                ?? throw new ArgumentException("VendedorId inválido.");

            if (!Enum.TryParse<MetodoPagamento>(dto.MetodoPagamento, true, out var metodo))
                throw new ArgumentException("MetodoPagamento inválido. Use: Pix, Boleto, Cartao.");

            var entity = new Pedido
            {
                VendedorId = dto.VendedorId,
                Valor = dto.Valor,
                MetodoPagamento = metodo,
                Produto = string.IsNullOrWhiteSpace(dto.Produto) ? null : dto.Produto.Trim(),
                Status = StatusPedido.Pendente,
                Criacao = DateTime.UtcNow
            };

            await _pedidoRepository.AddAsync(entity, ct);
            await _pedidoRepository.SaveChangesAsync(ct);

            return new PedidoDto
            {
                Id = entity.Id,
                Criacao = entity.Criacao,
                CriacaoBr = TimeUtils.ToBrazilOffset(entity.Criacao),
                DataPagamento = entity.DataPagamento,
                MetodoPagamento = entity.MetodoPagamento.ToString(),
                Valor = entity.Valor,
                VendedorId = entity.VendedorId,
                VendedorNome = vendedor.Nome,
                Produto = entity.Produto,
                Status = entity.Status
            };
        }

        public async Task<PedidosResponseDto> GetAllAsync(
            string? status, int? vendedorId, string? metodo, DateTime? dataInicio, DateTime? dataFim, int page, int pageSize, CancellationToken ct)
        {
            StatusPedido? statusPedido = null;
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<StatusPedido>(status, true, out var sParsed))
                statusPedido = sParsed;

            MetodoPagamento? metodoPagamento = null;
            if (!string.IsNullOrWhiteSpace(metodo) && Enum.TryParse<MetodoPagamento>(metodo, true, out var mParsed))
                metodoPagamento = mParsed;

            var count = await _pedidoRepository.GetCountAllAsync(statusPedido, vendedorId, metodoPagamento, dataInicio, dataFim, ct);
            var list = await _pedidoRepository.GetAllAsync(statusPedido, vendedorId, metodoPagamento, dataInicio, dataFim, page, pageSize, ct);

            return new PedidosResponseDto
            {
                Pedidos = list.Select(p => new PedidoDto
                {
                    Id = p.Id,
                    Criacao = p.Criacao,
                    CriacaoBr = TimeUtils.ToBrazilOffset(p.Criacao),
                    DataPagamento = p.DataPagamento,
                    MetodoPagamento = p.MetodoPagamento.ToString(),
                    Valor = p.Valor,
                    VendedorId = p.VendedorId,
                    VendedorNome = p.Vendedor?.Nome,
                    Produto = p.Produto,
                    Status = p.Status
                }),
                TotalRegistros = count
            };
        }

        public async Task<PedidoDto?> GetByIdAsync(int id, CancellationToken ct)
        {
            var p = await _pedidoRepository.GetByIdNoTrackingAsync(id, ct);
            if (p is null) return null;

            return new PedidoDto
            {
                Id = p.Id,
                Criacao = p.Criacao,
                CriacaoBr = TimeUtils.ToBrazilOffset(p.Criacao),
                DataPagamento = p.DataPagamento,
                MetodoPagamento = p.MetodoPagamento.ToString(),
                Valor = p.Valor,
                VendedorId = p.VendedorId,
                VendedorNome = p.Vendedor?.Nome,
                Produto = p.Produto,
                Status = p.Status
            };
        }

        public async Task<bool> UpdateStatusAsync(int id, PedidoStatusUpdateDto dto, CancellationToken ct)
        {
            var p = await _pedidoRepository.FindByIdAsync(id, ct);
            if (p is null) return false;

            p.Status = dto.Status;
            if (dto.Status == StatusPedido.Aprovado && p.DataPagamento is null)
                p.DataPagamento = DateTime.UtcNow;

            await _pedidoRepository.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> MarcarComoPagoAsync(int id, CancellationToken ct)
        {
            var p = await _pedidoRepository.FindByIdAsync(id, ct);
            if (p is null) return false;

            p.Status = StatusPedido.Aprovado;
            p.DataPagamento = p.DataPagamento ?? DateTime.UtcNow;

            await _pedidoRepository.SaveChangesAsync(ct);
            return true;
        }
    }
}