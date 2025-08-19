using Microsoft.EntityFrameworkCore;
using VersopayLibrary.Models;

namespace VersopayDatabase.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Documento> Documentos => Set<Documento>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var u = modelBuilder.Entity<Usuario>();
            u.HasKey(x => x.Id);
            u.Property(x => x.Nome).HasMaxLength(120).IsRequired();
            u.Property(x => x.Email).HasMaxLength(160).IsRequired();
            u.Property(x => x.SenhaHash).IsRequired();
            u.Property(x => x.CpfCnpj).HasMaxLength(14).IsRequired();
            u.HasIndex(x => x.Email).IsUnique();
            u.HasIndex(x => x.CpfCnpj).IsUnique();
            u.Property(x => x.IsAdmin).HasDefaultValue(false);
            u.ToTable(t => t.HasCheckConstraint(
                "CK_Usuarios_CpfCnpj_Tipo",
                "( (TipoCadastro = 0 AND LEN([CpfCnpj]) = 11) OR (TipoCadastro = 1 AND LEN([CpfCnpj]) = 14) )"
            ));

            var d = modelBuilder.Entity<Documento>();
            d.HasKey(x => x.UsuarioId);
            d.Property(x => x.FrenteRgCaminho).HasMaxLength(260);
            d.Property(x => x.VersoRgCaminho).HasMaxLength(260);
            d.Property(x => x.SelfieDocCaminho).HasMaxLength(260);
            d.Property(x => x.CartaoCnpjCaminho).HasMaxLength(260);

            d.Property(x => x.FrenteRgStatus).HasDefaultValue(StatusDocumento.Pendente);
            d.Property(x => x.VersoRgStatus).HasDefaultValue(StatusDocumento.Pendente);
            d.Property(x => x.SelfieDocStatus).HasDefaultValue(StatusDocumento.Pendente);
            d.Property(x => x.CartaoCnpjStatus).HasDefaultValue(StatusDocumento.Pendente);

            d.Property(x => x.FrenteRgAssinaturaSha256).HasMaxLength(64);
            d.Property(x => x.VersoRgAssinaturaSha256).HasMaxLength(64);
            d.Property(x => x.SelfieDocAssinaturaSha256).HasMaxLength(64);
            d.Property(x => x.CartaoCnpjAssinaturaSha256).HasMaxLength(64);

            u.HasOne(x => x.Documento)
             .WithOne(x => x.Usuario)
             .HasForeignKey<Documento>(x => x.UsuarioId)
             .OnDelete(DeleteBehavior.Cascade);

            var rt = modelBuilder.Entity<RefreshToken>();
            rt.HasKey(x => x.Id);
            rt.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            rt.HasIndex(x => x.TokenHash).IsUnique(); // lookup rápido por token
            rt.HasOne(x => x.Usuario)
              .WithMany() // ou crie ICollection<RefreshToken> no Usuario, se quiser navegar
              .HasForeignKey(x => x.UsuarioId)
              .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
