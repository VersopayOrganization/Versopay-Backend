namespace VersopayBackend.Dtos
{
    public sealed class VexyWithdrawReqDto
    {
        public decimal amount { get; set; }
        public string external_id { get; set; } = default!;
        public string pix_key { get; set; } = default!;
        public string key_type { get; set; } = default!; // EMAIL, CPF, CNPJ, PHONE
        public string? description { get; set; }
        public string clientCallbackUrl { get; set; } = default!;
    }
}
