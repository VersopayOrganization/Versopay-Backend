using VersopayBackend.Dtos;
using VersopayBackend.Repositories;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Services
{
    public sealed class FaturamentoService(
        IFaturamentoRepository faturamentoRepo,
        IUsuarioRepository usuarioRepo,
        IPedidoReadRepository pedidoReadRepo,
        IExtratoRepository extratoRepo
    ) : IFaturamentoService
    {
        public async Task<FaturamentoDto?> GetByIdAsync(int id, CancellationToken ct)
        {
            var f = await faturamentoRepo.GetByIdAsync(id, ct);
            return f?.ToDto();
        }

        public async Task<List<FaturamentoDto>> ListarAsync(string cpfCnpj, DateTime? inicioUtc, DateTime? fimUtc, CancellationToken ct)
        {
            var digits = new string((cpfCnpj ?? string.Empty).Where(char.IsDigit).ToArray());
            var list = await faturamentoRepo.ListByCpfCnpjAsync(digits, inicioUtc, fimUtc, ct);
            return list.Select(x => x.ToDto()).ToList();
        }

        public async Task<FaturamentoDto> RecalcularAsync(FaturamentoRecalcularRequest req, CancellationToken ct)
        {
            // Normaliza & valida documento
            var digits = new string((req.CpfCnpj ?? string.Empty).Where(char.IsDigit).ToArray());
            if (digits.Length != 11 && digits.Length != 14)
                throw new ArgumentException("CpfCnpj deve ter 11 (CPF) ou 14 (CNPJ) dígitos.");

            // Período
            var inicio = DateTime.SpecifyKind(req.DataInicio, DateTimeKind.Utc);
            var fim = DateTime.SpecifyKind(req.DataFim, DateTimeKind.Utc);
            if (fim <= inicio)
                throw new ArgumentException("Período inválido: DataFimUtc deve ser maior que DataInicioUtc.");

            // Descobre o usuário pelo CPF/CNPJ sem exigir método novo no repo
            var todosUsuarios = await usuarioRepo.GetAllNoTrackingAsync(ct);
            var usuario = todosUsuarios.FirstOrDefault(u => (u.CpfCnpj ?? "") == digits);
            int? vendedorId = usuario?.Id;

            // Agregações por método (usando métodos existentes do pedido repo)
            decimal vendasCartao = 0, vendasPix = 0, vendasBoleto = 0;

            if (vendedorId is not null)
            {
                // Agrega por método: usamos os aprovados
                var cartao = await pedidoReadRepo.GetStatsPorMetodoAsync(vendedorId.Value, MetodoPagamento.Cartao, inicio, fim, ct);
                var pix = await pedidoReadRepo.GetStatsPorMetodoAsync(vendedorId.Value, MetodoPagamento.Pix, inicio, fim, ct);
                var boleto = await pedidoReadRepo.GetStatsPorMetodoAsync(vendedorId.Value, MetodoPagamento.Boleto, inicio, fim, ct);

                vendasCartao = cartao.TotalAprovado;
                vendasPix = pix.TotalAprovado;
                vendasBoleto = boleto.TotalAprovado;
            }

            var vendasTotais = vendasCartao + vendasPix + vendasBoleto;

            // Reserva (se houver extrato)
            decimal reserva = 0m;
            if (vendedorId is not null)
            {
                var extrato = await extratoRepo.GetByClienteIdAsync(vendedorId.Value, ct);
                reserva = extrato?.ReservaFinanceira ?? 0m;
            }

            // Vendas canceladas e dias sem vendas:
            // Sem pedir novos métodos, usamos GetAllAsync com pageSize "alto".
            int vendasCanceladas = 0;
            int diasSemVendas = 0;

            if (vendedorId is not null)
            {
                // 1) Canceladas
                var cancelados = await pedidoReadRepo.GetAllAsync(
                    status: StatusPedido.Cancelado,
                    vendedorId: vendedorId.Value,
                    metodo: null,
                    dataDeUtc: inicio,
                    dataAteUtc: fim,
                    page: 1,
                    pageSize: 1_000_000,
                    cancellationToken: ct
                );
                vendasCanceladas = cancelados.Count;

                // 2) Dias sem vendas (considerando vendas aprovadas)
                var aprovados = await pedidoReadRepo.GetAllAsync(
                    status: StatusPedido.Aprovado,
                    vendedorId: vendedorId.Value,
                    metodo: null,
                    dataDeUtc: inicio,
                    dataAteUtc: fim,
                    page: 1,
                    pageSize: 1_000_000,
                    cancellationToken: ct
                );

                var diasComVenda = aprovados
                    .Select(p => DateOnly.FromDateTime(p.Criacao.Date))
                    .Distinct()
                    .Count();

                // intervalo em dias (início inclusivo, fim exclusivo)
                var totalDias = (int)Math.Max(0, (fim.Date - inicio.Date).TotalDays);
                diasSemVendas = Math.Max(0, totalDias - diasComVenda);
            }

            // Monta entidade e persiste (se Salvar = true)
            var entity = new Faturamento
            {
                CpfCnpj = digits,
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
                await faturamentoRepo.AddAsync(entity, ct);
                await faturamentoRepo.SaveChangesAsync(ct);
            }

            return entity.ToDto();
        }
    }
}
