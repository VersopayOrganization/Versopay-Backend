namespace VersopayBackend.Dtos
{
    public sealed class PerfilResumoDto
    {
        public string Nome { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string? Cpf { get; set; }
        public string? Cnpj { get; set; }
        public string? Telefone { get; set; }

        // opcionais (se você tiver essas colunas/tabela; se não, deixe null)
        public string? NomeFantasia { get; set; }
        public string? RazaoSocial { get; set; }
        public string? SiteOuRedeSocial { get; set; }
        public string? Instagram { get; set; }

        // vendas (lifetime)
        public int VendasQtd { get; set; }
        public decimal VendasTotal { get; set; }

        public TaxasDto Taxas { get; set; } = new();
    }
}
