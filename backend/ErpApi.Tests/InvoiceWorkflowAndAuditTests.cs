using System.Net;
using System.Net.Http.Json;
using ErpApi.Authorization;
using ErpApi.Data;
using ErpApi.Models;
using ErpApi.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ErpApi.Tests;

public class InvoiceWorkflowAndAuditTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;

    public InvoiceWorkflowAndAuditTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SubmitAndApproveInvoice_ValidTransitionsSucceed_AndWriteAuditEntries()
    {
        using var client = _factory.CreateAuthenticatedClient(
            TestData.AdminUserId,
            TestData.AdminUsername,
            PermissionConstants.InvoiceSubmit,
            PermissionConstants.InvoiceApprove);

        var submitResponse = await client.PostAsJsonAsync($"/api/invoices/{TestData.DraftInvoiceId}/submit", new { remarks = "submit" });
        var approveResponse = await client.PostAsJsonAsync($"/api/invoices/{TestData.DraftInvoiceId}/approve", new { remarks = "approve" });

        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        var submitLog = db.AuditLogs.FirstOrDefault(log => log.ActionType == "invoice.submit" && log.EntityId == TestData.DraftInvoiceId.ToString());
        var approveLog = db.AuditLogs.FirstOrDefault(log => log.ActionType == "invoice.approve" && log.EntityId == TestData.DraftInvoiceId.ToString());

        Assert.NotNull(submitLog);
        Assert.NotNull(approveLog);
        Assert.Equal(TestData.AdminUserId, submitLog!.UserId);
        Assert.Equal(TestData.AdminUsername, submitLog.Username);
        Assert.Contains("\"from\":\"Draft\"", submitLog.Metadata);
        Assert.Contains("\"to\":\"Submitted\"", submitLog.Metadata);
    }

    [Fact]
    public async Task ApproveInvoice_FromDraftState_Returns400()
    {
        using var client = _factory.CreateAuthenticatedClient(TestData.AdminUserId, TestData.AdminUsername, PermissionConstants.InvoiceApprove);

        var response = await client.PostAsJsonAsync($"/api/invoices/{TestData.DraftInvoiceId}/approve", new { remarks = "invalid" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ApprovedInvoice_CannotBeEditedOrDeleted()
    {
        using var client = _factory.CreateAuthenticatedClient(
            TestData.AdminUserId,
            TestData.AdminUsername,
            PermissionConstants.InvoiceWrite,
            PermissionConstants.InvoiceDelete);

        var updateRequest = new
        {
            invoiceNumber = "INV-APPROVED-001",
            companyId = TestData.CompanyAId,
            invoiceDate = DateTime.UtcNow,
            dueDate = DateTime.UtcNow.AddDays(5),
            status = InvoiceStatus.Sent,
            notes = "update should fail",
            items = new[]
            {
                new { description = "item", quantity = 1m, unitPrice = 100m, taxRate = 18m },
            },
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/invoices/{TestData.ApprovedInvoiceId}", updateRequest);
        var deleteResponse = await client.DeleteAsync($"/api/invoices/{TestData.ApprovedInvoiceId}");

        Assert.Equal(HttpStatusCode.Conflict, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task CreateCompany_WritesAuditLogWithActorAndEntityMetadata()
    {
        using var client = _factory.CreateAuthenticatedClient(TestData.AdminUserId, TestData.AdminUsername, PermissionConstants.CompanyWrite);
        var request = new
        {
            branchId = TestData.BranchAId,
            companyName = "Audit Co",
            address = "Address",
            city = "City",
            state = "State",
            pinCode = "123123",
            email = "audit@example.com",
            phoneNumber = "9999999999",
            website = "https://audit.example.com",
            gstin = "GSTINAUDIT",
            pan = "PANAUDIT",
            drugLicenceNumber = "DLAUDIT",
            udogAadhaar = "UDAUDIT",
            aadhaarNumber = "AADHAARAUDIT",
            msmeNumber = "MSMEAUDIT",
            fssaiNumber = "FSSAIAUDIT",
            updatedDate = DateTime.UtcNow,
            isActive = true,
        };

        var response = await client.PostAsJsonAsync("/api/companies", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        var companyCreateLog = db.AuditLogs
            .Where(log => log.ActionType == "company.create" && log.Username == TestData.AdminUsername)
            .OrderByDescending(log => log.CreatedDate)
            .FirstOrDefault();

        Assert.NotNull(companyCreateLog);
        Assert.Equal(nameof(Company), companyCreateLog!.EntityName);
        Assert.Equal(TestData.AdminUserId, companyCreateLog.UserId);
        Assert.Contains("Audit Co", companyCreateLog.NewValues);
    }
}
