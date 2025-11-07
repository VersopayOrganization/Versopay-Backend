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

    public enum ProvedorWebhook 
    { 
        VersellPay = 0, 
        VexyPayments = 1 
    }

    public enum WebhookEvento
    {
        Desconhecido = 0,
        DepositoPago = 10,     // Versell: PAID_OUT | Vexy: COMPLETED (depósito)
        Chargeback = 11,       // Versell: CHARGEBACK
        SaqueConcluido = 20,   // Vexy: COMPLETED (saque)
        SaqueRetidoMED = 21,    // Vexy: MED/RETIDO
        PagamentoPIX = 22,
        TransferenciaPIX = 23
    }

    public enum ProcessingStatus 
    { 
        Success = 0,
        Duplicate = 1, 
        InvalidAuth = 2,
        Error = 3 
    }
}
