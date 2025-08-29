namespace VersopayBackend.Dtos
{
    public sealed class WebhookTestPayloadDto
    {
        public string Tipo { get; set; } = "teste.webhook";
        public object? Dados { get; set; }
    }
}
