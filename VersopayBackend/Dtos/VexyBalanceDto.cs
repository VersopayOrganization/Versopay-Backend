namespace VersopayBackend.Dtos
{
    // Ajuste os nomes/campos conforme o contrato real da Vexy
    public sealed record VexyBalanceDto(
        decimal available,
        decimal blocked,
        decimal total
    );
}
