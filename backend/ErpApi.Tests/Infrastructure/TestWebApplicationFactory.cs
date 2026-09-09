using System.Net.Http.Headers;
using ErpApi.Controllers;
using ErpApi.Data;
using ErpApi.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ErpApi.Tests.Infrastructure;

public sealed class TestWebApplicationFactory : WebApplicationFactory<AuthController>, IAsyncLifetime
{
    private readonly string _databaseName = $"erp-tests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("UseInMemoryDatabase", "true");
        builder.UseSetting("InMemoryDatabaseName", _databaseName);
    }

    public async Task InitializeAsync()
    {
        await ResetDatabaseAsync();
    }

    public new Task DisposeAsync()
    {
        return base.DisposeAsync().AsTask();
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();

        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        await TestDataSeeder.SeedAsync(db);
    }

    public HttpClient CreateAuthenticatedClient(int userId, string username, params string[] permissions)
    {
        var client = CreateClient();
        var jwtSettings = Services.GetRequiredService<JwtSettings>();
        var token = TestJwtTokenFactory.CreateToken(jwtSettings, userId, username, permissions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
