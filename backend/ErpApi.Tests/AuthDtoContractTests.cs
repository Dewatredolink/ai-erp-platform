using System.Net;
using System.Net.Http.Json;
using ErpApi.DTOs.Auth;
using ErpApi.Tests.Infrastructure;
using Xunit;

namespace ErpApi.Tests;

public class AuthDtoContractTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;

    public AuthDtoContractTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Register_ReturnsUserSummaryResponse()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "new-user",
            email = "new-user@example.com",
            password = "Password123!"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<UserSummaryResponse>();
        Assert.NotNull(payload);
        Assert.Equal("new-user", payload!.Username);
        Assert.Equal("new-user@example.com", payload.Email);
        Assert.True(payload.UserId > 0);
    }

    [Fact]
    public async Task GetCurrentUser_ReturnsCurrentUserResponse()
    {
        using var client = _factory.CreateAuthenticatedClient(TestData.AdminUserId, TestData.AdminUsername);

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<CurrentUserResponse>();
        Assert.NotNull(payload);
        Assert.Equal(TestData.AdminUserId, payload!.UserId);
        Assert.Equal(TestData.AdminUsername, payload.Username);
        Assert.NotNull(payload.Roles);
        Assert.NotNull(payload.Permissions);
    }
}
