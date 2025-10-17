using VersopayLibrary.Models;

namespace VersopayBackend.Dtos
{
    public static class FaturamentoMapper
    {
        public static FaturamentoDto ToDto(this Faturamento faturamento) => new()
        {
            Id = faturamento.Id,
            Cpf = faturamento.Cpf,
            Cnpj = faturamento.Cnpj,
            DataInicio = faturamento.DataInicio,
            DataFim = faturamento.DataFim,
            VendasTotais = faturamento.VendasTotais,
            VendasCartao = faturamento.VendasCartao,
            VendasBoleto = faturamento.VendasBoleto,
            VendasPix = faturamento.VendasPix,
            Reserva = faturamento.Reserva,
            VendasCanceladas = faturamento.VendasCanceladas,
            DiasSemVendas = faturamento.DiasSemVendas,
            AtualizadoEmUtc = faturamento.AtualizadoEmUtc
        };
    }
}
