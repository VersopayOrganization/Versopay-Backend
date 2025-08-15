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
            u.HasKey(x => x.Id);
            u.Property(x => x.Nome).HasMaxLength(120).IsRequired();
            u.Property(x => x.Email).HasMaxLength(160).IsRequired();
            u.Property(x => x.SenhaHash).IsRequired();
            u.Property(x => x.CpfCnpj).HasMaxLength(14).IsRequired();

            u.HasIndex(x => x.Email).IsUnique();
            u.HasIndex(x => x.CpfCnpj).IsUnique();

            // SQL Server: LEN; (se usar SQLite, troque por LENGTH)
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

            // Relacionamento 1:1 (PK compartilhada) + cascade
            u.HasOne(x => x.Documento)
             .WithOne(x => x.Usuario)
             .HasForeignKey<Documento>(x => x.UsuarioId)
             .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
