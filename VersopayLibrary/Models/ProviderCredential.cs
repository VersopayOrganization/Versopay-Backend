using System;
using System.ComponentModel.DataAnnotations;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

public class ProviderCredential
{
    public int Id { get; set; }

    [Required] public int OwnerUserId { get; set; }
    public Usuario OwnerUser { get; set; } = default!;

    [Required] public PaymentProvider Provider { get; set; }

    [Required, MaxLength(200)] public string ClientId { get; set; } = default!;
    [Required, MaxLength(512)] public string ClientSecret { get; set; } = default!;

    // Vexy Bank
    public string? ApiKey { get; set; }        // pk_live_* ou pk_test_*
    public string? ApiSecret { get; set; }     // sk_live_* ou sk_test_*

    // Segredo atual usado para validar o HMAC do webhook
    [MaxLength(256)]
    public string? WebhookSignatureSecret { get; set; }

    // 🔄 Segredo anterior (permite período de transição/rotação)
    [MaxLength(256)]
    public string? PrevWebhookSignatureSecret { get; set; }

    // Cache do token de sessão (quando aplicável)
    [MaxLength(600)] public string? AccessToken { get; set; }
    public DateTime? AccessTokenExpiresUtc { get; set; }

    public DateTime CriadoEmUtc { get; set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEmUtc { get; set; }
}
