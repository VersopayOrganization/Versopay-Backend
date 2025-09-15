using VersopayBackend.Common;
using VersopayBackend.Dtos;
using VersopayBackend.Repositories;
using VersopayBackend.Services;
using VersopayBackend.Services.Taxas;
using VersopayBackend.Utils;
using VersopayLibrary.Enums;

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

            // vendas lifetime (aprovadas)
            var (qtd, total) = await pedidos.GetVendasAprovadasAsync(usuarioId, null, null, ct);

            string? cpf = u.TipoCadastro == VersopayLibrary.Models.TipoCadastro.PF ? u.CpfCnpj : null;
            string? cnpj = u.TipoCadastro == VersopayLibrary.Models.TipoCadastro.PJ ? u.CpfCnpj : null;

            return new PerfilResumoDto
            {
                Nome = u.Nome,
                Email = u.Email,
                Telefone = u.Telefone,
                Instagram = u.Instagram,
                // Se futuramente você tiver em outra entidade, preencha:
                NomeFantasia = null,
                RazaoSocial = null,
                SiteOuRedeSocial = null,
                Cpf = string.IsNullOrWhiteSpace(cpf) ? null : DocumentoFormatter.Mask(cpf),
                Cnpj = string.IsNullOrWhiteSpace(cnpj) ? null : DocumentoFormatter.Mask(cnpj),
                VendasQtd = qtd,
                VendasTotal = total,
                Taxas = fees.Get()
            };
        }

        public async Task<DashboardResumoDto> GetDashboardAsync(int usuarioId, DateTime deUtc, DateTime ateUtc, CancellationToken ct)
        {
            // faturamento no período (aprovados)
            var (_, fatPeriodo) = await pedidos.GetVendasAprovadasAsync(usuarioId, deUtc, ateUtc, ct);

            // extrato
            var extrato = await extratos.GetByClienteIdNoTrackingAsync(usuarioId, ct);

            // métricas por método
            var cartao = await pedidos.GetStatsPorMetodoAsync(usuarioId, MetodoPagamento.Cartao, deUtc, ateUtc, ct);
            var pix = await pedidos.GetStatsPorMetodoAsync(usuarioId, MetodoPagamento.Pix, deUtc, ateUtc, ct);
            var boleto = await pedidos.GetStatsPorMetodoAsync(usuarioId, MetodoPagamento.Boleto, deUtc, ateUtc, ct);

            var totalPedidosPeriodo = cartao.QtdTotal + pix.QtdTotal + boleto.QtdTotal;

            // chargeback / estorno
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
