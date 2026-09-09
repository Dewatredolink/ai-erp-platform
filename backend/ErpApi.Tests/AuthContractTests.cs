using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErpApi.Tests.Infrastructure;

namespace ErpApi.Tests;

public class AuthContractTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;

    public AuthContractTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Register_ReturnsStableUserSummaryContract()
    {
        using var client = _factory.CreateClient();
        var request = new
        {
            username = "contract-user",
            email = "contract-user@example.com",
            password = "Password123!",
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.TryGetProperty("userId", out var userId));
        Assert.True(userId.GetInt32() > 0);
        Assert.Equal("contract-user", json.RootElement.GetProperty("username").GetString());
        Assert.Equal("contract-user@example.com", json.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task GetCurrentUser_ReturnsStableCurrentUserContract()
    {
        using var client = _factory.CreateAuthenticatedClient(
            TestData.AdminUserId,
            TestData.AdminUsername,
            "invoice.read",
            "company.read");

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(TestData.AdminUserId, json.RootElement.GetProperty("userId").GetInt32());
        Assert.Equal(TestData.AdminUsername, json.RootElement.GetProperty("username").GetString());
        Assert.True(json.RootElement.TryGetProperty("roles", out var roles));
        Assert.Equal(JsonValueKind.Array, roles.ValueKind);
        Assert.True(json.RootElement.TryGetProperty("permissions", out var permissions));
        Assert.Equal(JsonValueKind.Array, permissions.ValueKind);
    }
}
