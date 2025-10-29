namespace VersopayBackend.Dtos
{
    public sealed class VexyWithdrawRespDto
    {
        public string transaction_id { get; set; } = default!;
        public string status { get; set; } = default!;
        public decimal amount { get; set; }
        public decimal? fee { get; set; }
        public string? ispb { get; set; }
        public string? nome_recebedor { get; set; }
        public string? cpf_recebedor { get; set; }
    }
}
