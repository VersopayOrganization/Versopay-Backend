namespace VersopayBackend.Utils
{
    public sealed class SmtpSettings
    {
        public string Host { get; set; } = "";
        public int Port { get; set; } = 587;
        public bool UseSsl { get; set; } = true;
        public string User { get; set; } = "";
        public string Pass { get; set; } = "";
        public string FromAddress { get; set; } = "";
        public string FromName { get; set; } = "VersoPay";
        public int TimeoutMs { get; set; } = 15000;
    }
}
