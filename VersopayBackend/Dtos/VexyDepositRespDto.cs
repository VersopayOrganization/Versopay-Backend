namespace VersopayBackend.Dtos
{
    public sealed class VexyDepositRespDto
    {
        public string transaction_id { get; set; } = default!;
        public string status { get; set; } = default!;
        public decimal amount { get; set; }
        public string? qr_code { get; set; }
        public string? qrcode_base64 { get; set; }
        public string? pix_copy_paste { get; set; }
    }
}
