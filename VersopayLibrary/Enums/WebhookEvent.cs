using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersopayLibrary.Enums
{
    [Flags]
    public enum WebhookEvent
    {
        None = 0,
        BoletoGerado = 1 << 0,
        PixGerado = 1 << 1,
        CompraAprovada = 1 << 2,
        CompraRecusada = 1 << 3,
        Estorno = 1 << 4,
        CarrinhoAbandonado = 1 << 5,
        Chargeback = 1 << 6,
        Processando = 1 << 7
    }
}
