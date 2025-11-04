// VersopayBackend/Dtos/VexyWebhookDto.cs
using System.Text.Json.Serialization;

namespace VersopayBackend.Dtos
{
    /// <summary>
    /// DTO resiliente para webhooks da Vexy:
    /// - Aceita payloads com campos "achatados"
    /// - Aceita payloads com objetos aninhados: { type: "transaction", event: "...", transaction: { ... } }
    ///   ou { type: "transfer", event: "...", transfer: { ... } }
    /// As propriedades públicas abaixo (transaction_id, status, amount...) fazem fallback automático.
    /// </summary>
    public sealed class VexyWebhookDto
    {
        // ---------------------------
        // Metadados de alto nível
        // ---------------------------
        public string? id { get; set; }                  // às vezes a Vexy envia um id na raiz
        public string? type { get; set; }                // "transaction" | "transfer"
        [JsonPropertyName("event")]
        public string? @event { get; set; }              // e.g., "transaction_paid", "transfer_completed"
        public string? scope { get; set; }               // "user" | "postback" etc.

        // Objetos aninhados (formatos alternativos)
        public TransactionPayload? transaction { get; set; }
        public TransferPayload? transfer { get; set; }

        // ---------------------------
        // Backing fields (achatado)
        // ---------------------------
        private string? _transaction_id;
        private string? _status;
        private decimal? _amount;
        private decimal? _fee;
        private decimal? _net_amount;
        private string? _ispb;
        private string? _nome_recebedor;
        private string? _cpf_recebedor;

        // ---------------------------
        // Propriedades esperadas pelo serviço (com fallback automático)
        // ---------------------------

        /// <summary>
        /// ID da transação/transferência (fallback: transaction.id -> transfer.id -> id da raiz)
        /// </summary>
        public string transaction_id
        {
            get => _transaction_id
                ?? transaction?.id
                ?? transfer?.id
                ?? id
                ?? string.Empty;
            set => _transaction_id = value;
        }

        /// <summary>
        /// Status (fallback: transaction.status -> transfer.status -> mapeado a partir do "event")
        /// </summary>
        public string status
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_status))
                    return _status!;

                var nested = transaction?.status ?? transfer?.status;
                if (!string.IsNullOrWhiteSpace(nested))
                    return nested!;

                // Mapeia alguns eventos comuns para um status equivalente
                return @event?.ToLowerInvariant() switch
                {
                    "transaction_paid" => "COMPLETED",
                    "transfer_completed" => "COMPLETED",
                    "transfer_canceled" => "CANCELED",
                    "transaction_refunded" => "REFUNDED",
                    _ => string.Empty
                };
            }
            set => _status = value;
        }

        /// <summary>
        /// Valor (em centavos ou decimal), fallback: transaction.amount -> transfer.amount
        /// </summary>
        public decimal amount
        {
            get => _amount
                ?? transaction?.amount
                ?? transfer?.amount
                ?? 0m;
            set => _amount = value;
        }

        /// <summary>
        /// Taxa (quando existir, geralmente em transfer)
        /// </summary>
        public decimal? fee
        {
            get => _fee ?? transfer?.fee;
            set => _fee = value;
        }

        /// <summary>
        /// Valor líquido (quando existir, geralmente em transfer)
        /// </summary>
        public decimal? net_amount
        {
            get => _net_amount ?? transfer?.net_amount;
            set => _net_amount = value;
        }

        /// <summary>
        /// ISPB (quando existir, geralmente em transfer)
        /// </summary>
        public string? ispb
        {
            get => _ispb ?? transfer?.ispb;
            set => _ispb = value;
        }

        /// <summary>
        /// Nome do recebedor (quando existir, geralmente em transfer)
        /// </summary>
        public string? nome_recebedor
        {
            get => _nome_recebedor ?? transfer?.nome_recebedor;
            set => _nome_recebedor = value;
        }

        /// <summary>
        /// CPF do recebedor (quando existir, geralmente em transfer)
        /// </summary>
        public string? cpf_recebedor
        {
            get => _cpf_recebedor ?? transfer?.cpf_recebedor;
            set => _cpf_recebedor = value;
        }

        // ============================
        // Tipos auxiliares (aninhados)
        // ============================

        public sealed class TransactionPayload
        {
            public string? id { get; set; }
            public decimal amount { get; set; }
            public string? status { get; set; }
            public string? description { get; set; }
            public TransactionPixInfo? pix { get; set; }
            public DateTime? paidAt { get; set; } // se existir
            public DateTime? createdAt { get; set; } // se existir

            public sealed class TransactionPixInfo
            {
                public string? endToEndId { get; set; }
                public PayerInfo? payerInfo { get; set; }

                public sealed class PayerInfo
                {
                    public string? bank { get; set; }
                    public string? name { get; set; }
                    public string? branch { get; set; }
                    public string? document { get; set; }
                    public string? account_type { get; set; }
                    public string? account_number { get; set; }
                }
            }
        }

        public sealed class TransferPayload
        {
            public string? id { get; set; }
            public decimal amount { get; set; }
            public string? status { get; set; }             // queued | processing | completed | canceled
            public decimal? fee { get; set; }
            public decimal? net_amount { get; set; }
            public string? ispb { get; set; }
            public string? nome_recebedor { get; set; }
            public string? cpf_recebedor { get; set; }
            public TransferPixInfo? pix { get; set; }
            public DateTime? createdAt { get; set; }  // se existir
            public DateTime? updatedAt { get; set; }  // se existir

            public sealed class TransferPixInfo
            {
                public string? endToEndId { get; set; }
                public object? creditorAccount { get; set; } // pode vir null; mantenha flexível
            }
        }
    }
}
