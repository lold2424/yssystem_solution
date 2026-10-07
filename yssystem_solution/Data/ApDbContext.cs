using Microsoft.EntityFrameworkCore;
using yssystem_solution.Entities;

namespace yssystem_solution.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Equipment> Equipments => Set<Equipment>();

        public DbSet<EquipLog> EquipLogs => Set<EquipLog>();

        private const string ConnectionString =
            "Server=localhost;Database=YSSYSTEM_SOLUTION;Trusted_Connection=True;TrustServerCertificate=True;";

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(ConnectionString);
            }
        }
    }
}