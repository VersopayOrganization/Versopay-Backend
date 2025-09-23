using Microsoft.EntityFrameworkCore;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayDatabase.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Documento> Documentos => Set<Documento>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<Pedido> Pedidos => Set<Pedido>();
        public DbSet<NovaSenhaResetToken> NovaSenhaResetTokens => Set<NovaSenhaResetToken>();
        public DbSet<KycKyb> KycKybs => Set<KycKyb>();
        public DbSet<UsuarioSenhaHistorico> UsuarioSenhasHistorico { get; set; }
        public DbSet<Antecipacao> Antecipacoes => Set<Antecipacao>();
        public DbSet<BypassToken> BypassTokens => Set<BypassToken>();
        public DbSet<DeviceTrustChallenge> DeviceTrustChallenges => Set<DeviceTrustChallenge>();
        public DbSet<Webhook> Webhooks => Set<Webhook>();
        public DbSet<Transferencia> Transferencias => Set<Transferencia>();
        public DbSet<Extrato> Extratos => Set<Extrato>();
        public DbSet<MovimentacaoFinanceira> MovimentacoesFinanceiras => Set<MovimentacaoFinanceira>();
        public DbSet<Faturamento> Faturamentos => Set<Faturamento>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            var usuario = modelBuilder.Entity<Usuario>();
            usuario.HasKey(x => x.Id);

            usuario.Property(x => x.Nome).HasMaxLength(120).IsRequired();
            usuario.Property(x => x.Email).HasMaxLength(160).IsRequired();
            usuario.Property(x => x.SenhaHash).IsRequired();
            usuario.Property(x => x.TipoCadastro).HasConversion<int?>();
            // CPF/CNPJ opcional no cadastro inicial
            usuario.Property(x => x.CpfCnpj).HasMaxLength(14);
            usuario.Property(x => x.IsAdmin).HasDefaultValue(false);
            usuario.HasIndex(x => x.Email).IsUnique();
            // unicidade do documento só quando houver valor
            usuario.HasIndex(x => x.CpfCnpj).IsUnique().HasFilter("[CpfCnpj] IS NOT NULL");

            usuario.Property(x => x.NomeFantasia).HasMaxLength(160);
            usuario.Property(x => x.RazaoSocial).HasMaxLength(160);
            usuario.Property(x => x.Site).HasMaxLength(160);

            usuario.Property(x => x.EnderecoCep).HasMaxLength(9);
            usuario.Property(x => x.EnderecoLogradouro).HasMaxLength(120);
            usuario.Property(x => x.EnderecoNumero).HasMaxLength(20);
            usuario.Property(x => x.EnderecoComplemento).HasMaxLength(80);
            usuario.Property(x => x.EnderecoBairro).HasMaxLength(80);
            usuario.Property(x => x.EnderecoCidade).HasMaxLength(80);
            usuario.Property(x => x.EnderecoUF).HasMaxLength(2);

            usuario.Property(x => x.NomeCompletoBanco).HasMaxLength(160);
            usuario.Property(x => x.CpfCnpjDadosBancarios).HasMaxLength(14);
            usuario.Property(x => x.ChavePix).HasMaxLength(120);
            usuario.Property(x => x.ChaveCarteiraCripto).HasMaxLength(120);

            // regra por tipo, mas permitindo o estado "inicial" (ambos nulos)
            usuario.ToTable(t => t.HasCheckConstraint(
                "CK_Usuarios_CpfCnpj_Tipo",
                "((TipoCadastro IS NULL AND [CpfCnpj] IS NULL) " +
                "OR (TipoCadastro = 0 AND LEN([CpfCnpj]) = 11) " +
                "OR (TipoCadastro = 1 AND LEN([CpfCnpj]) = 14))"
            ));

            var documento = modelBuilder.Entity<Documento>();
            documento.HasKey(x => x.UsuarioId);
            documento.Property(x => x.FrenteRgCaminho).HasMaxLength(260);
            documento.Property(x => x.VersoRgCaminho).HasMaxLength(260);
            documento.Property(x => x.SelfieDocCaminho).HasMaxLength(260);
            documento.Property(x => x.CartaoCnpjCaminho).HasMaxLength(260);

            documento.Property(x => x.FrenteRgStatus).HasDefaultValue(StatusDocumento.Pendente);
            documento.Property(x => x.VersoRgStatus).HasDefaultValue(StatusDocumento.Pendente);
            documento.Property(x => x.SelfieDocStatus).HasDefaultValue(StatusDocumento.Pendente);
            documento.Property(x => x.CartaoCnpjStatus).HasDefaultValue(StatusDocumento.Pendente);

            documento.Property(x => x.FrenteRgAssinaturaSha256).HasMaxLength(64);
            documento.Property(x => x.VersoRgAssinaturaSha256).HasMaxLength(64);
            documento.Property(x => x.SelfieDocAssinaturaSha256).HasMaxLength(64);
            documento.Property(x => x.CartaoCnpjAssinaturaSha256).HasMaxLength(64);

            usuario.HasOne(x => x.Documento)
             .WithOne(x => x.Usuario)
             .HasForeignKey<Documento>(x => x.UsuarioId)
             .OnDelete(DeleteBehavior.Cascade);


            var refreshToken = modelBuilder.Entity<RefreshToken>();
            refreshToken.HasKey(x => x.Id);
            refreshToken.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            refreshToken.HasIndex(x => x.TokenHash).IsUnique();
            refreshToken.HasOne(x => x.Usuario)
              .WithMany()
              .HasForeignKey(x => x.UsuarioId)
              .OnDelete(DeleteBehavior.Cascade);


            var pedido = modelBuilder.Entity<Pedido>();
            pedido.HasKey(x => x.Id);

            pedido.Property(x => x.Criacao).IsRequired();
            pedido.Property(x => x.DataPagamento);

            // enum gravado como string no BD
            pedido.Property(x => x.MetodoPagamento)
             .HasConversion<string>()
             .HasMaxLength(32)
             .IsRequired();

            pedido.Property(x => x.Valor)
             .HasColumnType("decimal(18,2)")
             .IsRequired();

            pedido.Property(x => x.Produto).HasMaxLength(200);

            pedido.Property(x => x.Status)
             .HasDefaultValue(StatusPedido.Pendente)
             .IsRequired();

            pedido.HasOne(x => x.Vendedor)
             .WithMany()
             .HasForeignKey(x => x.VendedorId)
             .OnDelete(DeleteBehavior.Restrict);

            pedido.Property(x => x.ExternalId).HasMaxLength(80);
            pedido.Property(x => x.GatewayTransactionId).HasMaxLength(80);
            pedido.HasIndex(x => x.ExternalId);
            pedido.HasIndex(x => x.GatewayTransactionId);

            // Índices úteis
            pedido.HasIndex(x => x.VendedorId);
            pedido.HasIndex(x => new { x.Status, x.Criacao });
            pedido.HasIndex(x => x.MetodoPagamento);


            var kycKyb = modelBuilder.Entity<KycKyb>();
            kycKyb.HasKey(x => x.Id);

            kycKyb.Property(x => x.Status).IsRequired();

            kycKyb.Property(x => x.CpfCnpj)
             .HasMaxLength(14)
             .IsRequired();

            kycKyb.Property(x => x.Nome)
             .HasMaxLength(120)
             .IsRequired();

            kycKyb.Property(x => x.NumeroDocumento)
             .HasMaxLength(64);

            kycKyb.Property(x => x.DataAprovacao);

            kycKyb.HasOne(x => x.Usuario)
             .WithMany()
             .HasForeignKey(x => x.UsuarioId)
             .OnDelete(DeleteBehavior.Cascade);

            // índices úteis
            kycKyb.HasIndex(x => x.UsuarioId);
            kycKyb.HasIndex(x => new { x.Status, x.UsuarioId });


            var antecipacao = modelBuilder.Entity<Antecipacao>();
            antecipacao.HasKey(x => x.Id);

            antecipacao.Property(x => x.DataSolicitacao).IsRequired();
            antecipacao.Property(x => x.Status).IsRequired();

            antecipacao.Property(x => x.Valor)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            antecipacao.HasOne(x => x.Empresa)
                .WithMany()
                .HasForeignKey(x => x.EmpresaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Índices úteis
            antecipacao.HasIndex(x => x.EmpresaId);
            antecipacao.HasIndex(x => x.Status);
            antecipacao.HasIndex(x => new { x.Status, x.DataSolicitacao });

            var webhook = modelBuilder.Entity<Webhook>();
            webhook.HasKey(x => x.Id);
            webhook.Property(x => x.Url).HasMaxLength(500).IsRequired();
            webhook.Property(x => x.Ativo).HasDefaultValue(true);
            webhook.Property(x => x.Secret).HasMaxLength(128);

            // converte enum flags para int
            webhook.Property(x => x.Eventos)
                   .HasConversion<int>()
                   .IsRequired();

            webhook.HasIndex(x => x.Ativo);
            webhook.HasIndex(x => x.Eventos);

            var inb = modelBuilder.Entity<InboundWebhookLog>();
            inb.HasKey(x => x.Id);
            inb.Property(x => x.EventKey).HasMaxLength(180).IsRequired();
            inb.HasIndex(x => x.EventKey).IsUnique();

            inb.Property(x => x.TransactionId).HasMaxLength(80);
            inb.Property(x => x.ExternalId).HasMaxLength(80);
            inb.Property(x => x.RequestNumber).HasMaxLength(80);
            inb.Property(x => x.Status).HasMaxLength(40);
            inb.Property(x => x.TipoTransacao).HasMaxLength(20);


            var bypassToken = modelBuilder.Entity<BypassToken>();
            bypassToken.HasKey(x => x.Id);
            bypassToken.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            bypassToken.HasIndex(x => x.TokenHash).IsUnique();
            bypassToken.Property(x => x.Ip).HasMaxLength(64);
            bypassToken.Property(x => x.UserAgent).HasMaxLength(200);
            bypassToken.Property(x => x.Dispositivo).HasMaxLength(80);

            bypassToken.HasOne(x => x.Usuario)
              .WithMany()
              .HasForeignKey(x => x.UsuarioId)
              .OnDelete(DeleteBehavior.Cascade);

            bypassToken.HasIndex(x => new { x.UsuarioId, x.RevogadoEmUtc, x.ExpiraEmUtc });


            var deviceTrust = modelBuilder.Entity<DeviceTrustChallenge>();
            deviceTrust.ToTable("DeviceTrustChallenges");
            deviceTrust.HasKey(x => x.Id);
            deviceTrust.Property(x => x.CodeHash).HasMaxLength(128).IsRequired();
            deviceTrust.Property(x => x.Ip).HasMaxLength(64);
            deviceTrust.Property(x => x.UserAgent).HasMaxLength(200);
            deviceTrust.Property(x => x.Dispositivo).HasMaxLength(80);

            deviceTrust.HasIndex(x => new { x.UsuarioId, x.Used, x.ExpiresAtUtc });

            deviceTrust.HasOne(x => x.Usuario)
              .WithMany()
              .HasForeignKey(x => x.UsuarioId)
              .OnDelete(DeleteBehavior.Cascade);

            var transferencia = modelBuilder.Entity<Transferencia>();
            transferencia.HasKey(x => x.Id);

            transferencia.Property(x => x.Status).IsRequired();
            transferencia.Property(x => x.DataSolicitacao).IsRequired();
            transferencia.Property(x => x.DataCadastro).IsRequired();

            transferencia.Property(x => x.ValorSolicitado)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            transferencia.Property(x => x.Taxa).HasColumnType("decimal(18,2)");
            transferencia.Property(x => x.ValorFinal).HasColumnType("decimal(18,2)");

            transferencia.Property(x => x.Nome).HasMaxLength(120);
            transferencia.Property(x => x.Empresa).HasMaxLength(160);
            transferencia.Property(x => x.ChavePix).HasMaxLength(120);

            transferencia.Property(x => x.ExternalId).HasMaxLength(80);
            transferencia.Property(x => x.GatewayTransactionId).HasMaxLength(80);
            transferencia.HasIndex(x => x.ExternalId);
            transferencia.HasIndex(x => x.GatewayTransactionId);

            transferencia.HasOne(x => x.Solicitante)
                .WithMany()
                .HasForeignKey(x => x.SolicitanteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Índices
            transferencia.HasIndex(x => x.SolicitanteId);
            transferencia.HasIndex(x => new { x.Status, x.DataSolicitacao });

            var extrato = modelBuilder.Entity<Extrato>();
            extrato.HasKey(x => x.Id);
            extrato.Property(x => x.SaldoDisponivel).HasColumnType("decimal(18,2)");
            extrato.Property(x => x.SaldoPendente).HasColumnType("decimal(18,2)");
            extrato.Property(x => x.ReservaFinanceira).HasColumnType("decimal(18,2)");
            extrato.Property(x => x.AtualizadoEmUtc).IsRequired();

            extrato.HasOne(x => x.Cliente)
                   .WithMany() // 1:1 lógico — não precisa coleção em Usuario
                   .HasForeignKey(x => x.ClienteId)
                   .OnDelete(DeleteBehavior.Cascade);

            extrato.HasIndex(x => x.ClienteId).IsUnique(); // um extrato por cliente

            var movimentacaoFinanceira = modelBuilder.Entity<MovimentacaoFinanceira>();
            movimentacaoFinanceira.HasKey(x => x.Id);
            movimentacaoFinanceira.Property(x => x.Tipo).IsRequired();
            movimentacaoFinanceira.Property(x => x.Status).IsRequired();
            movimentacaoFinanceira.Property(x => x.Valor).HasColumnType("decimal(18,2)").IsRequired();
            movimentacaoFinanceira.Property(x => x.Descricao).HasMaxLength(200);
            movimentacaoFinanceira.Property(x => x.Referencia).HasMaxLength(80);

            movimentacaoFinanceira.HasOne(x => x.Cliente)
               .WithMany()
               .HasForeignKey(x => x.ClienteId)
               .OnDelete(DeleteBehavior.Cascade);

            movimentacaoFinanceira.HasIndex(x => new { x.ClienteId, x.Status, x.CriadoEmUtc });

            var faturamento = modelBuilder.Entity<Faturamento>();
            faturamento.HasKey(x => x.Id);

            faturamento.Property(x => x.CpfCnpj).HasMaxLength(14).IsRequired();

            faturamento.Property(x => x.VendasTotais).HasColumnType("decimal(18,2)");
            faturamento.Property(x => x.VendasCartao).HasColumnType("decimal(18,2)");
            faturamento.Property(x => x.VendasBoleto).HasColumnType("decimal(18,2)");
            faturamento.Property(x => x.VendasPix).HasColumnType("decimal(18,2)");
            faturamento.Property(x => x.Reserva).HasColumnType("decimal(18,2)");

            faturamento.Property(x => x.DataInicio).IsRequired();
            faturamento.Property(x => x.DataFim).IsRequired();
            faturamento.Property(x => x.AtualizadoEmUtc).IsRequired();

            faturamento.HasIndex(x => x.CpfCnpj);
            faturamento.HasIndex(x => new { x.CpfCnpj, x.DataInicio, x.DataFim });
        }
    }
}
