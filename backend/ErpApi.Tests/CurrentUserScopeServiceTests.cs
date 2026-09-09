using System.Security.Claims;
using ErpApi.Data;
using ErpApi.Services;
using ErpApi.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ErpApi.Tests;

public class CurrentUserScopeServiceTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;

    public CurrentUserScopeServiceTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ResolveBranchForCompanyCreateAsync_UsesSingleAssignedBranchWhenRequestIsMissing()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        var service = new CurrentUserScopeService(
            db,
            CreateCurrentUserService(TestData.ScopedUserId, TestData.ScopedUsername));

        var result = await service.ResolveBranchForCompanyCreateAsync(null);

        Assert.True(result.IsAuthorized);
        Assert.Equal(TestData.BranchAId, result.BranchId);
        Assert.NotNull(result.Branch);
        Assert.Equal(TestData.BranchAId, result.Branch!.BranchId);
    }

    [Fact]
    public async Task CheckCompanyAccessAsync_ReturnsExistingForbiddenCompanyForScopedUser()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        var service = new CurrentUserScopeService(
            db,
            CreateCurrentUserService(TestData.ScopedUserId, TestData.ScopedUsername));

        var result = await service.CheckCompanyAccessAsync(TestData.CompanyBId);

        Assert.False(result.HasAccess);
        Assert.True(result.Exists);
    }

    private static CurrentUserService CreateCurrentUserService(int userId, string username)
    {
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                        new Claim("userId", userId.ToString()),
                        new Claim(ClaimTypes.Name, username),
                    ],
                    authenticationType: "Test"))
            }
        };

        return new CurrentUserService(accessor);
    }
}
