using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

public class ProviderCredential
{
    public int Id { get; set; }

    [Required] public int OwnerUserId { get; set; }
    public Usuario OwnerUser { get; set; } = default!;

    [Required] public PaymentProvider Provider { get; set; }

    [Required, MaxLength(120)] public string ClientId { get; set; } = default!;
    [Required, MaxLength(160)] public string ClientSecret { get; set; } = default!;

    // Só a Vexy usa token de sessão (JWT); Versell usa headers fixos por request
    [MaxLength(600)] public string? AccessToken { get; set; }
    public DateTime? AccessTokenExpiresUtc { get; set; }

    public DateTime CriadoEmUtc { get; set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEmUtc { get; set; }
}

