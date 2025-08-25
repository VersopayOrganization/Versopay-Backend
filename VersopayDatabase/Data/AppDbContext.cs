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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            var usuario = modelBuilder.Entity<Usuario>();
            usuario.HasKey(x => x.Id);

            usuario.Property(x => x.Nome).HasMaxLength(120).IsRequired();
            usuario.Property(x => x.Email).HasMaxLength(160).IsRequired();
            usuario.Property(x => x.SenhaHash).IsRequired();
            // enum nullable -> int? no banco
            usuario.Property(x => x.TipoCadastro).HasConversion<int?>();
            // CPF/CNPJ opcional no cadastro inicial
            usuario.Property(x => x.CpfCnpj).HasMaxLength(14); // sem .IsRequired()
            usuario.Property(x => x.IsAdmin).HasDefaultValue(false);
            // índices
            usuario.HasIndex(x => x.Email).IsUnique();
            // unicidade do documento só quando houver valor
            usuario.HasIndex(x => x.CpfCnpj).IsUnique().HasFilter("[CpfCnpj] IS NOT NULL"); // SQL Server

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
            refreshToken.HasIndex(x => x.TokenHash).IsUnique(); // lookup rápido por token
            refreshToken.HasOne(x => x.Usuario)
              .WithMany() // ou crie ICollection<RefreshToken> no Usuario, se quiser navegar
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
             .WithMany() // se quiser, coloque ICollection<Pedido> em Usuario
             .HasForeignKey(x => x.VendedorId)
             .OnDelete(DeleteBehavior.Restrict);

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

            kycKyb.Property(x => x.DataAprovacao); // datetime2 null

            kycKyb.HasOne(x => x.Usuario)
             .WithMany() // deixe sem coleção, ou crie ICollection<KycKyb> no Usuario se quiser
             .HasForeignKey(x => x.UsuarioId)
             .OnDelete(DeleteBehavior.Cascade);

            // índices úteis
            kycKyb.HasIndex(x => x.UsuarioId);
            kycKyb.HasIndex(x => new { x.Status, x.UsuarioId });
        }
    }
}
