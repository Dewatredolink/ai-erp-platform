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
        public DbSet<User> Users { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Company>()
                .HasKey(c => c.CompanyId);

            modelBuilder.Entity<Company>()
                .Property(c => c.CompanyName)
                .IsRequired();

            modelBuilder.Entity<User>()
                .HasKey(u => u.UserId);

            modelBuilder.Entity<User>()
                .Property(u => u.Username)
                .IsRequired();

            modelBuilder.Entity<User>()
                .Property(u => u.Email)
                .IsRequired();

            modelBuilder.Entity<User>()
                .Property(u => u.PasswordHash)
                .IsRequired();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Invoice>()
                .HasKey(i => i.InvoiceId);

            modelBuilder.Entity<Invoice>()
                .Property(i => i.InvoiceNumber)
                .IsRequired();

            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.InvoiceNumber)
                .IsUnique();

            modelBuilder.Entity<Invoice>()
                .Property(i => i.TotalAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Invoice>()
                .Property(i => i.TaxAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Invoice>()
                .Property(i => i.GrandTotal)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Company)
                .WithMany()
                .HasForeignKey(i => i.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InvoiceItem>()
                .HasKey(ii => ii.InvoiceItemId);

            modelBuilder.Entity<InvoiceItem>()
                .Property(ii => ii.Description)
                .IsRequired();

            modelBuilder.Entity<InvoiceItem>()
                .Property(ii => ii.Quantity)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<InvoiceItem>()
                .Property(ii => ii.UnitPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<InvoiceItem>()
                .Property(ii => ii.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<InvoiceItem>()
                .Property(ii => ii.TaxRate)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<InvoiceItem>()
                .Property(ii => ii.TaxAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<InvoiceItem>()
                .HasOne(ii => ii.Invoice)
                .WithMany(i => i.Items)
                .HasForeignKey(ii => ii.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
