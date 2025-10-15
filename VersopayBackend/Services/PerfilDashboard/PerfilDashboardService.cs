using Microsoft.Extensions.Options;
using VersopayBackend.Common;
using VersopayBackend.Dtos;
using VersopayBackend.Options;
using VersopayBackend.Repositories;
using VersopayBackend.Utils;
using VersopayLibrary.Enums;

namespace VersopayBackend.Services.PerfilDashboard
{
    public sealed class PerfilDashboardService(
       IUsuarioRepository usuarios,
       IPedidoReadRepository pedidos,
       IExtratoRepository extratos,
       IOptions<TaxasOptions> taxasOptions
   ) : IPerfilDashboardService
    {
        public async Task<PerfilResponseDto> GetPerfilAsync(int usuarioId, CancellationToken cancellationToken)
        {
            var usuario = await usuarios.GetByIdNoTrackingAsync(usuarioId, cancellationToken)
                    ?? throw new ArgumentException("Usuário inválido.");

            var (qtdAprovados, totalAprovados) = await pedidos.GetVendasAprovadasAsync(usuarioId, null, null, cancellationToken);

            var cpf = DocumentoFormatter.Mask(usuario.Cpf);
            var cnpj = DocumentoFormatter.Mask(usuario.Cnpj);

            return new PerfilResponseDto
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                Telefone = usuario.Telefone,
                Instagram = usuario.Instagram,

                Cpf = cpf,
                Cnpj = cnpj,

                QtdeVendasAprovadas = qtdAprovados,
                TotalVendidoAprovado = totalAprovados,
                Taxas = new TaxasDto
                {
                    CartaoPorcentagem = taxasOptions.Value.CartaoPorcentagem,
                    PixEntradaPorcentagem = taxasOptions.Value.PixEntradaPorcentagem,
                    PixSaidaPorcentagem = taxasOptions.Value.PixSaidaPorcentagem,
                    BoletoPorcentagem = taxasOptions.Value.BoletoPorcentagem
                },

                NomeFantasia = null,
                RazaoSocial = null,
                SiteOuRede = null
            };
        }

        // ASSINATURA EXATA: int, DashboardQueryDto, CancellationToken
        public async Task<DashboardResponseDto> GetDashboardAsync(int usuarioId, DashboardQueryDto dashboardQueryDto, CancellationToken cancellationToken)
        {
            var (qtdAprovados, totalAprovados) = await pedidos.GetVendasAprovadasAsync(
                usuarioId, dashboardQueryDto.DataInicio, dashboardQueryDto.DataFim, cancellationToken);

            var extrato = await extratos.GetByClienteIdNoTrackingAsync(usuarioId, cancellationToken);
            var saldoDisponivel = extrato?.SaldoDisponivel ?? 0m;
            var saldoPendente = extrato?.SaldoPendente ?? 0m;

            var cartao = await pedidos.GetStatsPorMetodoAsync(usuarioId, MetodoPagamento.Cartao, dashboardQueryDto.DataInicio, dashboardQueryDto.DataFim, cancellationToken);
            var pix = await pedidos.GetStatsPorMetodoAsync(usuarioId, MetodoPagamento.Pix, dashboardQueryDto.DataInicio, dashboardQueryDto.DataFim, cancellationToken);
            var boleto = await pedidos.GetStatsPorMetodoAsync(usuarioId, MetodoPagamento.Boleto, dashboardQueryDto.DataInicio, dashboardQueryDto.DataFim, cancellationToken);

            var (chargebackQuantidade, chargebackTotal) = await pedidos.GetChargebackAsync(usuarioId, dashboardQueryDto.DataInicio, dashboardQueryDto.DataFim, cancellationToken);
            var aprovadosPeriodo = cartao.QtdAprovado + pix.QtdAprovado + boleto.QtdAprovado;
            var chargebackPorcentagem = aprovadosPeriodo > 0 ? (decimal)chargebackQuantidade / aprovadosPeriodo * 100m : 0m;

            return new DashboardResponseDto
            {
                Faturamento = totalAprovados,
                SaldoDisponivel = saldoDisponivel,
                SaldoPendente = saldoPendente,

                Cartao = MapMetodo("Cartao", cartao),
                Pix = MapMetodo("Pix", pix),
                Boleto = MapMetodo("Boleto", boleto),

                Chargeback = new ChargebackStatsDto
                {
                    Percent = decimal.Round(chargebackPorcentagem, 1),
                    Qtde = chargebackQuantidade,
                    Total = chargebackTotal
                }
            };
        }

        private static MetodoStatsDto MapMetodo(string nome, MetodoStatsRaw metodoStatusDto)
        {
            var rate = metodoStatusDto.QtdTotal > 0 ? (decimal)metodoStatusDto.QtdAprovado / metodoStatusDto.QtdTotal * 100m : 0m;
            return new MetodoStatsDto
            {
                Metodo = nome,
                AprovacaoPercent = decimal.Round(rate, 1),
                QtdePedidos = metodoStatusDto.QtdTotal,
                TotalPedidos = metodoStatusDto.Total,
                QtdeAprovados = metodoStatusDto.QtdAprovado,
                TotalAprovados = metodoStatusDto.TotalAprovado
            };
        }
    }
}
