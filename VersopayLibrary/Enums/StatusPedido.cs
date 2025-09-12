using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersopayLibrary.Enums
{
    public enum StatusPedido
    {
        // --- Pré-pagamento ---
        Pendente = 0,  // boleto/pix aguardando pagamento; cartão aguardando processamento
        Expirado = 1,  // boleto/pix expirou
        Cancelado = 2,  // cancelado antes do pagamento/captura

        // --- Análise / risco ---
        Processando = 10, // análise antifraude/adquirente em andamento
        Recusado = 11, // recusado pelo antifraude/adquirente/banco emissor

        // --- Cartão: etapas específicas ---
        Autorizado = 20, // autorização OK (valor “reservado” no cartão)
        Capturado = 21, // captura realizada (cobrança efetivada no cartão)

        // --- Confirmação de pagamento (agnóstico ao método) ---
        //     Use estes estados para sinalizar “venda aprovada/paga”
        Pago = 30,  // pix recebido / boleto compensado / cartão capturado
        Aprovado = 30,  // ALIAS de Pago (mantém compatibilidade com código antigo)
        Concluido = 31,  // aprovado no meio de pagamento, mas ainda NÃO compensado na plataforma
        Liquidado = 32,  // valor compensado/liquidado na plataforma (entra no saldo disponível)

        // --- Pós-pagamento ---
        EstornoParcial = 40,  // reembolso parcial
        Estornado = 41,  // reembolso/estorno total
        Chargeback = 42   // contestação (chargeback)
    }
}

