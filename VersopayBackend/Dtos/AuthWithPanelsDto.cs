namespace VersopayBackend.Dtos
{
    public sealed class AuthWithPanelsDto
    {
        public AuthResponseDto Auth { get; set; } = default!;
        public PerfilResumoDto Perfil { get; set; } = default!;
        public DashboardResumoDto Dashboard { get; set; } = default!;
        public TaxasDto Taxas { get; set; } = default!;
    }
}
