namespace VersopayBackend.Dtos
{
    public sealed record UploadUrlsRequest(string[] Parts);

    public sealed record ConfirmDocumentoDto(
        string? FrenteRgCaminho,
        string? VersoRgCaminho,
        string? SelfieDocCaminho,
        string? CartaoCnpjCaminho
    );
}
