using ErpApi.Models;

namespace ErpApi.Tests.Infrastructure;

internal static class TestData
{
    internal static readonly Guid DraftInvoiceId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    internal static readonly Guid ApprovedInvoiceId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    internal const int BranchAId = 10;
    internal const int BranchBId = 20;
    internal const int CompanyAId = 1000;
    internal const int CompanyBId = 2000;

    internal const int AdminUserId = 100;
    internal const int ScopedUserId = 101;
    internal const int NoPermissionUserId = 102;

    internal const string AdminUsername = "admin-user";
    internal const string ScopedUsername = "scoped-user";
    internal const string NoPermissionUsername = "no-permission-user";

    internal static Invoice CreateInvoice(Guid invoiceId, string invoiceNumber, int companyId, InvoiceApprovalStatus approvalStatus, InvoiceStatus status, string createdBy)
    {
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
        var item = new InvoiceItem
        {
            InvoiceItemId = Guid.NewGuid(),
            InvoiceId = invoiceId,
            Description = "Item",
            Quantity = 1,
            UnitPrice = 100,
            Amount = 100,
            TaxRate = 18,
            TaxAmount = 18,
            CreatedDate = now,
        };

        return new Invoice
        {
            InvoiceId = invoiceId,
            InvoiceNumber = invoiceNumber,
            CompanyId = companyId,
            InvoiceDate = now,
            DueDate = now.AddDays(7),
            Status = status,
            ApprovalStatus = approvalStatus,
            CreatedBy = createdBy,
            UpdatedBy = createdBy,
            CreatedDate = now,
            UpdatedDate = now,
            TotalAmount = 100,
            TaxAmount = 18,
            GrandTotal = 118,
            Items = [item],
        };
    }
}
