using ErpApi.Authorization;
using ErpApi.Data;
using ErpApi.Models;

namespace ErpApi.Tests.Infrastructure;

internal static class TestDataSeeder
{
    internal static async Task SeedAsync(ErpDbContext db)
    {
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

        db.Branches.AddRange(
            new Branch { BranchId = TestData.BranchAId, Name = "Branch A", Code = "A", CreatedDate = now, UpdatedDate = now, IsActive = true },
            new Branch { BranchId = TestData.BranchBId, Name = "Branch B", Code = "B", CreatedDate = now, UpdatedDate = now, IsActive = true });

        db.Companies.AddRange(
            new Company
            {
                CompanyId = TestData.CompanyAId,
                BranchId = TestData.BranchAId,
                CompanyName = "Company A",
                Address = "Address A",
                City = "City",
                State = "State",
                PinCode = "123456",
                Email = "a@example.com",
                PhoneNumber = "1111111111",
                Website = "https://a.example.com",
                GSTIN = "GSTINA",
                PAN = "PANA",
                DrugLicenceNumber = "DLA",
                UdogAadhaar = "UA",
                AadhaarNumber = "AADHAARA",
                MSMENumber = "MSMEA",
                FSSAINumber = "FSSAIA",
                CreatedBy = TestData.AdminUsername,
                UpdatedBy = TestData.AdminUsername,
                CreatedDate = now,
                UpdatedDate = now,
                IsActive = true,
            },
            new Company
            {
                CompanyId = TestData.CompanyBId,
                BranchId = TestData.BranchBId,
                CompanyName = "Company B",
                Address = "Address B",
                City = "City",
                State = "State",
                PinCode = "654321",
                Email = "b@example.com",
                PhoneNumber = "2222222222",
                Website = "https://b.example.com",
                GSTIN = "GSTINB",
                PAN = "PANB",
                DrugLicenceNumber = "DLB",
                UdogAadhaar = "UB",
                AadhaarNumber = "AADHAARB",
                MSMENumber = "MSMEB",
                FSSAINumber = "FSSAIB",
                CreatedBy = TestData.AdminUsername,
                UpdatedBy = TestData.AdminUsername,
                CreatedDate = now,
                UpdatedDate = now,
                IsActive = true,
            });

        db.Users.AddRange(
            new User
            {
                UserId = TestData.AdminUserId,
                Username = TestData.AdminUsername,
                Email = "admin@example.com",
                PasswordHash = "hashed",
                CreatedDate = now,
                UpdatedDate = now,
                IsActive = true,
            },
            new User
            {
                UserId = TestData.ScopedUserId,
                Username = TestData.ScopedUsername,
                Email = "scoped@example.com",
                PasswordHash = "hashed",
                CreatedDate = now,
                UpdatedDate = now,
                IsActive = true,
            },
            new User
            {
                UserId = TestData.NoPermissionUserId,
                Username = TestData.NoPermissionUsername,
                Email = "noperm@example.com",
                PasswordHash = "hashed",
                CreatedDate = now,
                UpdatedDate = now,
                IsActive = true,
            });

        db.UserRoles.Add(new UserRole
        {
            UserId = TestData.AdminUserId,
            RoleId = 1,
            CreatedDate = now,
        });

        db.UserCompanies.Add(new UserCompany
        {
            UserId = TestData.ScopedUserId,
            CompanyId = TestData.CompanyAId,
            CreatedDate = now,
        });

        db.UserBranches.Add(new UserBranch
        {
            UserId = TestData.ScopedUserId,
            BranchId = TestData.BranchAId,
            CreatedDate = now,
        });

        db.Invoices.AddRange(
            TestData.CreateInvoice(
                TestData.DraftInvoiceId,
                "INV-DRAFT-001",
                TestData.CompanyAId,
                InvoiceApprovalStatus.Draft,
                InvoiceStatus.Draft,
                TestData.AdminUsername),
            TestData.CreateInvoice(
                TestData.ApprovedInvoiceId,
                "INV-APPROVED-001",
                TestData.CompanyAId,
                InvoiceApprovalStatus.Approved,
                InvoiceStatus.Sent,
                TestData.AdminUsername));

        await db.SaveChangesAsync();
    }
}
