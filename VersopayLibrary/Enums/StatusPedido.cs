using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersopayLibrary.Enums
{
    public enum StatusPedido
    {
        Pendente = 0,
        Pago = 1,
        Recusado = 2,
        Cancelado = 3,
        Processando = 4,
        Reembolsado = 5,
        Chargeback = 6
    }

}
