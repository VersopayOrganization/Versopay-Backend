namespace VersopayBackend.Dtos
{
    public sealed class VersellWebhookDto
    {
        public string requestNumber { get; set; } = default!;
        public string statusTransaction { get; set; } = default!; // PAID_OUT, CHARGEBACK
        public string idTransaction { get; set; } = default!;
        public string typeTransaction { get; set; } = default!;   // PIX
        public decimal value { get; set; }
        public string debtorName { get; set; } = default!;
        public string debtorDocument { get; set; } = default!;
        public DateTime date { get; set; }
    }

}
