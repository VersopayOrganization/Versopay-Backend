namespace VersopayBackend.Dtos
{
    public sealed class VexyMedLogDto
    {
        public DateTime timestamp { get; set; }
        public string webhook_type { get; set; } = "MED";
        public string transaction_id { get; set; } = default!;
        public string status { get; set; } = "RETIDO";
        public string? source_ip { get; set; }
        public DateTime? processed_at { get; set; }
        public string? processing_status { get; set; }
    }
}
