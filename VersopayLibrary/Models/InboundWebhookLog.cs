// VersopayLibrary/Models/InboundWebhookLog.cs
using VersopayLibrary.Enums;

namespace VersopayLibrary.Models
{
    public class InboundWebhookLog
    {
        public long Id { get; set; }

        // Quem enviou
        public ProvedorWebhook Provedor { get; set; }
        public WebhookEvento Evento { get; set; }

        // Idempotência (UNIQUE)
        public string EventKey { get; set; } = default!; // ex: "vexy:trx123:COMPLETED"

        // Dados comuns
        public string? TransactionId { get; set; }       // idTransaction / transaction_id
        public string? ExternalId { get; set; }          // seu id externo (se houver)
        public string? RequestNumber { get; set; }       // Versell
        public string? Status { get; set; }              // status bruto
        public string? TipoTransacao { get; set; }       // PIX, etc.

        public decimal? Valor { get; set; }
        public decimal? Fee { get; set; }
        public decimal? NetAmount { get; set; }
        public string? DebtorName { get; set; }
        public string? DebtorDocument { get; set; }
        public string? Ispb { get; set; }
        public string? NomeRecebedor { get; set; }
        public string? CpfRecebedor { get; set; }

        public DateTime? DataEventoUtc { get; set; }

        // request info
        public string SourceIp { get; set; } = "";
        public string HeadersJson { get; set; } = "{}";
        public string PayloadJson { get; set; } = "{}";

        // processamento
        public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAtUtc { get; set; }
        public ProcessingStatus ProcessingStatus { get; set; } = ProcessingStatus.Success;
        public string? ProcessingError { get; set; }

        // vínculos (opcional)
        public int? PedidoId { get; set; }
        public int? TransferenciaId { get; set; }
    }
}
