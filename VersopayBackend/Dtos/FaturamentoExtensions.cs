using VersopayBackend.Dtos;
using VersopayLibrary.Models;

namespace VersopayBackend
{
    public static class FaturamentoExtensions
    {
        public static FaturamentoDto ToDto(this Faturamento f) => new()
        {
            Id = f.Id,
            Cpf = f.Cpf ?? string.Empty,
            Cnpj = f.Cnpj ?? string.Empty,
            DataInicio = f.DataInicio,
            DataFim = f.DataFim,
            VendasTotais = f.VendasTotais,
            VendasCartao = f.VendasCartao,
            VendasBoleto = f.VendasBoleto,
            VendasPix = f.VendasPix,
            Reserva = f.Reserva,
            VendasCanceladas = f.VendasCanceladas,
            DiasSemVendas = f.DiasSemVendas,
            AtualizadoEmUtc = f.AtualizadoEmUtc
        };
    }
}
