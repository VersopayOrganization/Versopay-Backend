// VersopayLibrary/Models/VexyBankPixIn.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace VersopayLibrary.Models
{
    /// <summary>
    /// Registro local de um PIX IN criado na VexyBank.
    /// Correlaciona o fluxo: criação -> webhook -> consulta.
    /// </summary>
    public class VexyBankPixIn
    {
        public int Id { get; set; }

        [Required] public int OwnerUserId { get; set; }

        /// <summary>ID externo retornado pela Vexy (ex.: "cmhnd0tb900qt1mib0ihtatrw").</summary>
        [Required, MaxLength(100)]
        public string ExternalId { get; set; } = default!;

        /// <summary>Status Vexy: pending | paid | completed | canceled | expired | ...</summary>
        [MaxLength(40)]
        public string? Status { get; set; }

        /// <summary>Valor em centavos (preencha se você tiver no momento da criação).</summary>
        public long? AmountCents { get; set; }

        /// <summary>Payload EMV do QR.</summary>
        public string? PixEmv { get; set; }

        /// <summary>QR em PNG base64 (se retornado).</summary>
        public string? QrPngBase64 { get; set; }

        /// <summary>URL de postback usada na criação.</summary>
        public string? PostbackUrl { get; set; }

        /// <summary>CPF/CNPJ do pagador (preenchido no webhook).</summary>
        public string? PayerDocument { get; set; }
        /// <summary> Adicionar o vínculo com o pedido para facilitar reconciliação </summary>
        public int? PedidoId { get; set; }


        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
        public DateTime? PaidAtUtc { get; set; }
    }
}
