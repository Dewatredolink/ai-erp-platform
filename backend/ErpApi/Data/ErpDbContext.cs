using Microsoft.EntityFrameworkCore;
using ErpApi.Models;

namespace ErpApi.Data
{
    public class ErpDbContext : DbContext
    {
        private static readonly DateTime SeedTimestamp = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public ErpDbContext(DbContextOptions<ErpDbContext> options) : base(options)
        {
        }

        public DbSet<Company> Companies { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

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

            modelBuilder.Entity<User>()
                .HasMany(u => u.UserRoles)
                .WithOne(ur => ur.User)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

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

            modelBuilder.Entity<Role>()
                .HasKey(r => r.RoleId);

            modelBuilder.Entity<Role>()
                .Property(r => r.Name)
                .IsRequired();

            modelBuilder.Entity<Role>()
                .HasIndex(r => r.Name)
                .IsUnique();

            modelBuilder.Entity<Permission>()
                .HasKey(p => p.PermissionId);

            modelBuilder.Entity<Permission>()
                .Property(p => p.Name)
                .IsRequired();

            modelBuilder.Entity<Permission>()
                .HasIndex(p => p.Name)
                .IsUnique();

            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AuditLog>()
                .HasKey(a => a.AuditLogId);

            modelBuilder.Entity<AuditLog>()
                .Property(a => a.ActionType)
                .IsRequired();

            modelBuilder.Entity<AuditLog>()
                .Property(a => a.EntityName)
                .IsRequired();

            modelBuilder.Entity<AuditLog>()
                .Property(a => a.Username)
                .IsRequired();

            modelBuilder.Entity<AuditLog>()
                .Property(a => a.CorrelationId)
                .IsRequired();

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.CreatedDate);

            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Role>().HasData(new Role
            {
                RoleId = 1,
                Name = "Administrator",
                Description = "Default system administrator role",
                CreatedDate = SeedTimestamp,
            });

            modelBuilder.Entity<Permission>().HasData(
                new Permission { PermissionId = 1, Name = "company.read", Description = "Read companies", CreatedDate = SeedTimestamp },
                new Permission { PermissionId = 2, Name = "company.write", Description = "Create and update companies", CreatedDate = SeedTimestamp },
                new Permission { PermissionId = 3, Name = "company.delete", Description = "Delete companies", CreatedDate = SeedTimestamp },
                new Permission { PermissionId = 4, Name = "invoice.read", Description = "Read invoices", CreatedDate = SeedTimestamp },
                new Permission { PermissionId = 5, Name = "invoice.write", Description = "Create and update draft invoices", CreatedDate = SeedTimestamp },
                new Permission { PermissionId = 6, Name = "invoice.delete", Description = "Delete invoices", CreatedDate = SeedTimestamp },
                new Permission { PermissionId = 7, Name = "audit.read", Description = "Read audit logs", CreatedDate = SeedTimestamp },
                new Permission { PermissionId = 8, Name = "invoice.submit", Description = "Submit invoices for approval", CreatedDate = SeedTimestamp },
                new Permission { PermissionId = 9, Name = "invoice.approve", Description = "Approve submitted invoices", CreatedDate = SeedTimestamp },
                new Permission { PermissionId = 10, Name = "invoice.reject", Description = "Reject submitted invoices", CreatedDate = SeedTimestamp },
                new Permission { PermissionId = 11, Name = "invoice.pay", Description = "Mark approved invoices as paid", CreatedDate = SeedTimestamp });

            modelBuilder.Entity<RolePermission>().HasData(
                new RolePermission { RoleId = 1, PermissionId = 1, CreatedDate = SeedTimestamp },
                new RolePermission { RoleId = 1, PermissionId = 2, CreatedDate = SeedTimestamp },
                new RolePermission { RoleId = 1, PermissionId = 3, CreatedDate = SeedTimestamp },
                new RolePermission { RoleId = 1, PermissionId = 4, CreatedDate = SeedTimestamp },
                new RolePermission { RoleId = 1, PermissionId = 5, CreatedDate = SeedTimestamp },
                new RolePermission { RoleId = 1, PermissionId = 6, CreatedDate = SeedTimestamp },
                new RolePermission { RoleId = 1, PermissionId = 7, CreatedDate = SeedTimestamp },
                new RolePermission { RoleId = 1, PermissionId = 8, CreatedDate = SeedTimestamp },
                new RolePermission { RoleId = 1, PermissionId = 9, CreatedDate = SeedTimestamp },
                new RolePermission { RoleId = 1, PermissionId = 10, CreatedDate = SeedTimestamp },
                new RolePermission { RoleId = 1, PermissionId = 11, CreatedDate = SeedTimestamp });
        }
    }
}
