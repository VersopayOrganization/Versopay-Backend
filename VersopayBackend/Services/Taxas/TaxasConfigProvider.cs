using Microsoft.Extensions.Options;
using VersopayBackend.Dtos;
using VersopayBackend.Options;

namespace VersopayBackend.Services.Taxas
{
    public sealed class TaxasConfigProvider(IOptions<TaxasOptions> opt) : ITaxasProvider
    {
        public TaxasDto Get() => new()
        {
            CartaoPorcentagem = opt.Value.CartaoPorcentagem,
            PixEntradaPorcentagem = opt.Value.PixEntradaPorcentagem,
            PixSaidaPorcentagem = opt.Value.PixSaidaPorcentagem,
            PixSaidaFixo = opt.Value.PixSaidaFixo,
            BoletoPorcentagem = opt.Value.BoletoPorcentagem,
            BoletoFixo = opt.Value.BoletoFixo
        };
    }
}
