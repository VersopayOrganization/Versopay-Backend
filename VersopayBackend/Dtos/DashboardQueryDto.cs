namespace VersopayBackend.Dtos
{
    // DASHBOARD (cliente → dashboard)
    public sealed class DashboardQueryDto
    {
        // opcional: período (UTC)
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
    }

}
