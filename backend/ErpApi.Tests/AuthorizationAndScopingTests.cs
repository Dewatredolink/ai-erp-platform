using System.Net;
using System.Net.Http.Json;
using ErpApi.Authorization;
using ErpApi.DTOs.Companies;
using ErpApi.Tests.Infrastructure;
using Xunit;

namespace ErpApi.Tests;

public class AuthorizationAndScopingTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;

    public AuthorizationAndScopingTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetCompanies_WithoutToken_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/companies");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCompanies_WithAuthenticatedButUnauthorizedUser_Returns403()
    {
        using var client = _factory.CreateAuthenticatedClient(TestData.NoPermissionUserId, TestData.NoPermissionUsername);

        var response = await client.GetAsync("/api/companies");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminUser_CanAccessProtectedCompanyInvoiceAndAuditEndpoints()
    {
        using var client = _factory.CreateAuthenticatedClient(TestData.AdminUserId, TestData.AdminUsername, PermissionConstants.CompanyRead, PermissionConstants.InvoiceRead, PermissionConstants.AuditRead);

        var companiesResponse = await client.GetAsync("/api/companies");
        var invoicesResponse = await client.GetAsync("/api/invoices");
        var auditResponse = await client.GetAsync("/api/audit-logs");

        Assert.Equal(HttpStatusCode.OK, companiesResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, invoicesResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);
    }

    [Fact]
    public async Task ScopedUser_CanAccessAssignedCompany_ButNotOtherCompany()
    {
        using var client = _factory.CreateAuthenticatedClient(TestData.ScopedUserId, TestData.ScopedUsername, PermissionConstants.CompanyRead);

        var allowedResponse = await client.GetAsync($"/api/companies/{TestData.CompanyAId}");
        var deniedResponse = await client.GetAsync($"/api/companies/{TestData.CompanyBId}");

        Assert.Equal(HttpStatusCode.OK, allowedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deniedResponse.StatusCode);
    }

    [Fact]
    public async Task ScopedUser_CannotCreateInvoiceForOutsideCompany()
    {
        using var client = _factory.CreateAuthenticatedClient(TestData.ScopedUserId, TestData.ScopedUsername, PermissionConstants.InvoiceWrite);
        var request = new
        {
            invoiceNumber = "INV-SCOPE-001",
            companyId = TestData.CompanyBId,
            invoiceDate = DateTime.UtcNow,
            dueDate = DateTime.UtcNow.AddDays(7),
            status = "Draft",
            notes = "scope test",
            items = new[]
            {
                new { description = "item", quantity = 1m, unitPrice = 100m, taxRate = 18m },
            },
        };

        var response = await client.PostAsJsonAsync("/api/invoices", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ScopedUser_CreateCompanyWithoutBranch_AssignsOnlyAllowedBranch()
    {
        using var client = _factory.CreateAuthenticatedClient(TestData.ScopedUserId, TestData.ScopedUsername, PermissionConstants.CompanyWrite);
        var request = new
        {
            companyName = "Scoped Branch Company",
            address = "Address",
            city = "City",
            state = "State",
            pinCode = "123123",
            email = "scoped-branch@example.com",
            phoneNumber = "9999999999",
            website = "https://scoped-branch.example.com",
            gstin = "GSTINSCOPED",
            pan = "PANSCOPED",
            drugLicenceNumber = "DLSCOPED",
            udogAadhaar = "UDSCOPED",
            aadhaarNumber = "AADHAARSCOPED",
            msmeNumber = "MSMESCOPED",
            fssaiNumber = "FSSAISCOPED",
            isActive = true,
        };

        var response = await client.PostAsJsonAsync("/api/companies", request);
        var payload = await response.Content.ReadFromJsonAsync<CompanyResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal(TestData.BranchAId, payload!.BranchId);
    }
}
