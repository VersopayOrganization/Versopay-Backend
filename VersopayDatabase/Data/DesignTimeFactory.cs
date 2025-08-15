using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VersopayDatabase.Data
{
    // Permite gerar migrations mesmo o projeto sendo class library
    public class DesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                // ajuste a connection abaixo se quiser rodar migrations diretamente neste projeto
                .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=VersopayDb;Trusted_Connection=True;MultipleActiveResultSets=true")
                //.UseSqlite("Data Source=versopay.db")
                .Options;

            return new AppDbContext(options);
        }
    }
}

