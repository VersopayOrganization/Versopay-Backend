namespace VersopayBackend.Dtos
{
    public sealed record AuthWithPanelsResult(AuthWithPanelsDto Payload, string RefreshRaw, DateTime RefreshExpiresUtc);

}
