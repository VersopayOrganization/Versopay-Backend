using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersopayLibrary.Enums
{
    // Você pode adicionar novos métodos sem mexer no schema (vamos gravar como string)
    public enum MetodoPagamento
    {
        Pix = 0,
        Boleto = 1,
        Cartao = 2
        // Futuro: Ted, Transferencia, etc.
    }
}
