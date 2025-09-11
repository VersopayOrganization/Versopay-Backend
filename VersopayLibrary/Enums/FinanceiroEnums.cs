using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersopayLibrary.Enums
{
    public class FinanceiroEnums
    {
        public enum TipoMovimentacao { Credito = 0, Debito = 1 }
        public enum StatusMovimentacao { Pendente = 0, Efetivada = 1, Cancelada = 2 }
    }
}
