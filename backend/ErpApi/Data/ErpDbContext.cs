using Microsoft.EntityFrameworkCore;
using ErpApi.Models;

namespace ErpApi.Data
{
    public class ErpDbContext : DbContext
    {
        public ErpDbContext(DbContextOptions<ErpDbContext> options) : base(options)
        {
        }

        public DbSet<Company> Companies { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Company>()
                .HasKey(c => c.CompanyId);

            modelBuilder.Entity<Company>()
                .Property(c => c.CompanyName)
                .IsRequired();
        }
    }
}