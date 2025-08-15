using Microsoft.EntityFrameworkCore;
using VersopayLibrary.Models;

namespace VersopayDatabase.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Documento> Documentos => Set<Documento>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var u = modelBuilder.Entity<Usuario>();
            u.Property(p => p.Nome).HasMaxLength(120).IsRequired();
            u.Property(p => p.Email).HasMaxLength(160).IsRequired();
            u.Property(p => p.SenhaHash).IsRequired();
            u.Property(p => p.CpfCnpj).HasMaxLength(14).IsRequired();

            u.HasIndex(p => p.Email).IsUnique();
            u.HasIndex(p => p.CpfCnpj).IsUnique();

            // CHECK: 11 dígitos para PF, 14 para PJ (SQL Server: LEN; SQLite: LENGTH)
            u.ToTable(t => t.HasCheckConstraint(
                "CK_Usuarios_CpfCnpj_Tipo",
                "( (TipoCadastro = 0 AND LEN([CpfCnpj]) = 11) OR (TipoCadastro = 1 AND LEN([CpfCnpj]) = 14) )"
            ));

            var d = modelBuilder.Entity<Documento>();
            d.HasKey(p => p.UsuarioId);
            d.Property(p => p.FrenteRgCnhPath).HasMaxLength(260);
            d.Property(p => p.VersoRgCnhPath).HasMaxLength(260);
            d.Property(p => p.SelfieComDocPath).HasMaxLength(260);
            d.Property(p => p.CartaoCnpjPdfPath).HasMaxLength(260);

            // 1:1 Usuario <-> Documento (PK compartilhada)
            u.HasOne(x => x.Documento)
             .WithOne(x => x.Usuario)
             .HasForeignKey<Documento>(x => x.UsuarioId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
