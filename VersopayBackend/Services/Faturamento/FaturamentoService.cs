using VersopayBackend.Dtos;
using VersopayBackend.Repositories;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Services
{
    public sealed class FaturamentoService : IFaturamentoService
    {
        private readonly IFaturamentoRepository _faturamentoRepo;
        private readonly IUsuarioRepository _usuarioRepo;
        private readonly IPedidoReadRepository _pedidoReadRepo;
        private readonly IExtratoRepository _extratoRepo;

        public FaturamentoService(
            IFaturamentoRepository faturamentoRepo,
            IUsuarioRepository usuarioRepo,
            IPedidoReadRepository pedidoReadRepo,
            IExtratoRepository extratoRepo)
        {
            _faturamentoRepo = faturamentoRepo;
            _usuarioRepo = usuarioRepo;
            _pedidoReadRepo = pedidoReadRepo;
            _extratoRepo = extratoRepo;
        }

        public async Task<FaturamentoDto?> GetByIdAsync(int id, CancellationToken ct)
        {
            var f = await _faturamentoRepo.GetByIdAsync(id, ct);
            return f?.ToDto();
        }

        public async Task<List<FaturamentoDto>> ListarAsync(string cpfCnpj, DateTime? inicioUtc, DateTime? fimUtc, CancellationToken ct)
        {
            var digits = new string((cpfCnpj ?? string.Empty).Where(char.IsDigit).ToArray());
            var list = await _faturamentoRepo.ListByCpfCnpjAsync(digits, inicioUtc, fimUtc, ct);
            return list.Select(x => x.ToDto()).ToList();
        }

        public async Task<FaturamentoDto> RecalcularAsync(FaturamentoRecalcularRequest req, CancellationToken ct)
        {
            var digits = new string((req.CpfCnpj ?? string.Empty).Where(char.IsDigit).ToArray());
            if (digits.Length != 11 && digits.Length != 14)
                throw new ArgumentException("CpfCnpj deve ter 11 (CPF) ou 14 (CNPJ) dígitos.");

            var inicio = DateTime.SpecifyKind(req.DataInicio, DateTimeKind.Utc);
            var fim = DateTime.SpecifyKind(req.DataFim, DateTimeKind.Utc);
            if (fim <= inicio)
                throw new ArgumentException("Período inválido: DataFimUtc deve ser maior que DataInicioUtc.");

            var usuarios = await _usuarioRepo.GetAllNoTrackingAsync(ct);
            var usuario = usuarios.FirstOrDefault(u => (u.Cpf == digits) || (u.Cnpj == digits));
            int? vendedorId = usuario?.Id;

            decimal vendasCartao = 0, vendasPix = 0, vendasBoleto = 0;

            if (vendedorId is not null)
            {
                var cartao = await _pedidoReadRepo.GetStatsPorMetodoAsync(vendedorId.Value, MetodoPagamento.Cartao, inicio, fim, ct);
                var pix = await _pedidoReadRepo.GetStatsPorMetodoAsync(vendedorId.Value, MetodoPagamento.Pix, inicio, fim, ct);
                var boleto = await _pedidoReadRepo.GetStatsPorMetodoAsync(vendedorId.Value, MetodoPagamento.Boleto, inicio, fim, ct);

                vendasCartao = cartao.TotalAprovado;
                vendasPix = pix.TotalAprovado;
                vendasBoleto = boleto.TotalAprovado;
            }

            var vendasTotais = vendasCartao + vendasPix + vendasBoleto;

            decimal reserva = 0m;
            if (vendedorId is not null)
            {
                var extrato = await _extratoRepo.GetByClienteIdAsync(vendedorId.Value, ct);
                reserva = extrato?.ReservaFinanceira ?? 0m;
            }

            int vendasCanceladas = 0;
            int diasSemVendas = 0;

            if (vendedorId is not null)
            {
                var cancelados = await _pedidoReadRepo.GetAllAsync(
                    status: StatusPedido.Cancelado,
                    vendedorId: vendedorId.Value,
                    metodo: null,
                    dataInicio: inicio,
                    dataFim: fim,
                    page: 1,
                    pageSize: 1_000_000,
                    ct);

                vendasCanceladas = cancelados.Count;

                var aprovados = await _pedidoReadRepo.GetAllAsync(
                    status: StatusPedido.Aprovado,
                    vendedorId: vendedorId.Value,
                    metodo: null,
                    dataInicio: inicio,
                    dataFim: fim,
                    page: 1,
                    pageSize: 1_000_000,
                    ct);

                var diasComVenda = aprovados
                    .Select(p => DateOnly.FromDateTime(p.Criacao.Date))
                    .Distinct()
                    .Count();

                var totalDias = (int)Math.Max(0, (fim.Date - inicio.Date).TotalDays);
                diasSemVendas = Math.Max(0, totalDias - diasComVenda);
            }

            var entity = new Faturamento
            {
                Cpf = digits.Length == 11 ? digits : null,
                Cnpj = digits.Length == 14 ? digits : null,
                DataInicio = inicio,
                DataFim = fim,
                VendasTotais = vendasTotais,
                VendasCartao = vendasCartao,
                VendasBoleto = vendasBoleto,
                VendasPix = vendasPix,
                Reserva = reserva,
                VendasCanceladas = vendasCanceladas,
                DiasSemVendas = diasSemVendas,
                AtualizadoEmUtc = DateTime.UtcNow
            };

            if (req.Salvar)
            {
                await _faturamentoRepo.AddAsync(entity, ct);
                await _faturamentoRepo.SaveChangesAsync(ct);
            }

            return entity.ToDto();
        }
    }
}