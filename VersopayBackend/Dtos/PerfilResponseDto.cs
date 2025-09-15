namespace VersopayBackend.Dtos
{
    // PERFIL (cliente → perfil)
    public sealed class PerfilResponseDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string? Cpf { get; set; }
        public string? Cnpj { get; set; }
        public string? Telefone { get; set; }
        public string? Instagram { get; set; }

        // vendas
        public int QtdeVendasAprovadas { get; set; }
        public decimal TotalVendidoAprovado { get; set; }

        // taxas (vindo de appsettings por enquanto)
        public TaxasDto Taxas { get; set; } = new();

        // dados “empresa/social” (deixa null por enquanto se não existir em Usuario)
        public string? NomeFantasia { get; set; }
        public string? RazaoSocial { get; set; }
        public string? SiteOuRede { get; set; }
    }
}
