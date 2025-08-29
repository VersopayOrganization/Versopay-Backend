namespace VersopayBackend.Options
{
    public sealed class SmtpSettings
    {
        public string Host { get; set; } = default!;
        public int Port { get; set; }
        public string User { get; set; } = default!;
        public string Pass { get; set; } = default!;
        public string FromName { get; set; } = "VersoPay";
        public string FromAddress { get; set; } = default!;
        public bool UseSsl { get; set; } = true;
    }
}