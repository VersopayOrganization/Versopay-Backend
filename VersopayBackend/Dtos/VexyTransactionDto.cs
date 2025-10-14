using System;

namespace VersopayBackend.Dtos
{
    // Ajuste os nomes/campos conforme o contrato real da Vexy
    public sealed record VexyTransactionDto(
        string id,
        string status,
        decimal amount,
        decimal? fee,
        decimal? net_amount,
        string? ispb,
        string? nome_recebedor,
        string? cpf_recebedor,
        DateTime? created_at,
        DateTime? completed_at
    );
}
