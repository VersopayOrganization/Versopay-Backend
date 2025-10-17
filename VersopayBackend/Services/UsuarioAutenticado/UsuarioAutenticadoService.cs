using VersopayBackend.Common;
using VersopayBackend.Dtos;
using VersopayBackend.Repositories;
using VersopayBackend.Services;
using VersopayBackend.Services.Taxas;
using VersopayBackend.Utils;
using VersopayLibrary.Enums; // <- enum correto

namespace VersopayBackend.Services
{
    public sealed class UsuarioAutenticadoService(
        IUsuarioRepository usuarios,
        IPedidoReadRepository pedidos,
        IExtratoRepository extratos,
        ITaxasProvider fees,
        IClock clock) : IUsuarioAutenticadoService
    {
        public async Task<PerfilResumoDto> GetPerfilAsync(int usuarioId, CancellationToken ct)
        {
            var u = await usuarios.GetByIdNoTrackingAsync(usuarioId, ct)
                    ?? throw new InvalidOperationException("Usuário não encontrado.");

            var (qtd, total) = await pedidos.GetVendasAprovadasAsync(usuarioId, null, null, ct);

            // agora vem direto de Usuario.Cpf / Usuario.Cnpj
            var cpfMasked = string.IsNullOrWhiteSpace(u.Cpf) ? null : DocumentoFormatter.Mask(u.Cpf);
            var cnpjMasked = string.IsNullOrWhiteSpace(u.Cnpj) ? null : DocumentoFormatter.Mask(u.Cnpj);

            return new PerfilResumoDto
            {
                Nome = u.Nome,
                Email = u.Email,
                Telefone = u.Telefone,
                Instagram = u.Instagram,

                NomeFantasia = null,
                RazaoSocial = null,
                SiteOuRedeSocial = null,

                Cpf = cpfMasked,
                Cnpj = cnpjMasked,
                VendasQtd = qtd,
                VendasTotal = total,
                Taxas = fees.Get()
            };
        }

        public async Task<DashboardResumoDto> GetDashboardAsync(int usuarioId, DateTime deUtc, DateTime ateUtc, CancellationToken ct)
        {
            var (_, fatPeriodo) = await pedidos.GetVendasAprovadasAsync(usuarioId, deUtc, ateUtc, ct);

            var extrato = await extratos.GetByClienteIdNoTrackingAsync(usuarioId, ct);

            var cartao = await pedidos.GetStatsPorMetodoAsync(usuarioId, VersopayLibrary.Enums.MetodoPagamento.Cartao, deUtc, ateUtc, ct);
            var pix = await pedidos.GetStatsPorMetodoAsync(usuarioId, VersopayLibrary.Enums.MetodoPagamento.Pix, deUtc, ateUtc, ct);
            var boleto = await pedidos.GetStatsPorMetodoAsync(usuarioId, VersopayLibrary.Enums.MetodoPagamento.Boleto, deUtc, ateUtc, ct);

            var totalPedidosPeriodo = cartao.QtdTotal + pix.QtdTotal + boleto.QtdTotal;

            var (qtdCbk, totalCbk) = await pedidos.GetChargebackAsync(usuarioId, deUtc, ateUtc, ct);
            var cbkPercent = totalPedidosPeriodo == 0 ? 0m : Math.Round((decimal)qtdCbk / totalPedidosPeriodo * 100m, 1);

            return new DashboardResumoDto
            {
                FaturamentoPeriodo = fatPeriodo,
                SaldoDisponivel = extrato?.SaldoDisponivel ?? 0m,
                SaldoPendente = extrato?.SaldoPendente ?? 0m,
                PeriodoDeUtc = deUtc,
                PeriodoAteUtc = ateUtc,

                Cartao = Build(cartao),
                Pix = Build(pix),
                Boleto = Build(boleto),

                Chargeback = new ChargebackResumoDto
                {
                    Qtd = qtdCbk,
                    Total = totalCbk,
                    PercentualSobreTotalPedidos = cbkPercent
                }
            };

            static MetodoAprovacaoDto Build(MetodoStatsRaw m) => new()
            {
                QtdTotal = m.QtdTotal,
                QtdAprovado = m.QtdAprovado,
                Total = m.Total,
                TotalAprovado = m.TotalAprovado,
                PercentAprovacao = m.QtdTotal == 0 ? 0m : Math.Round((decimal)m.QtdAprovado / m.QtdTotal * 100m, 1)
            };
        }
    }
}
