using Microsoft.EntityFrameworkCore;
using VersopayLibrary.Models;

namespace VersopayDatabase.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Usuario> Usuarios => Set<Usuario>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var u = modelBuilder.Entity<Usuario>();

            u.Property(p => p.Nome).HasMaxLength(120).IsRequired();
            u.Property(p => p.Email).HasMaxLength(160).IsRequired();
            u.Property(p => p.SenhaHash).IsRequired();
            u.Property(p => p.CpfCnpj).HasMaxLength(14).IsRequired();

            u.HasIndex(p => p.Email).IsUnique();
            u.HasIndex(p => p.CpfCnpj).IsUnique();

            // (Opcional – SQL Server) Regra de tamanho por tipo
            u.ToTable(t => t.HasCheckConstraint(
                "CK_Usuarios_Documento_Tipo",
                "( (TipoCadastro = 0 AND LEN([Documento]) = 11) OR (TipoCadastro = 1 AND LEN([Documento]) = 14) )"
            ));
        }
    }
}
