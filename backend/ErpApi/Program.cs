using System.Text;
using ErpApi.Authorization;
using ErpApi.Data;
using ErpApi.Middleware;
using ErpApi.Services;
using ErpApi.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<InvoicePdfService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<CurrentUserScopeService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            NameClaimType = System.Security.Claims.ClaimTypes.Name,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        };
    });

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in PermissionConstants.All)
    {
        options.AddPolicy(permission, policy =>
            policy.RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission)));
    }
});
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

var useInMemoryDatabase = builder.Configuration.GetValue<bool>("UseInMemoryDatabase");
if (useInMemoryDatabase)
{
    var inMemoryDatabaseName = builder.Configuration.GetValue<string>("InMemoryDatabaseName") ?? "ErpApiTestDb";
    builder.Services.AddDbContext<ErpDbContext>(options => options.UseInMemoryDatabase(inMemoryDatabaseName));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Host=localhost;Port=5432;Database=erp_db;Username=postgres;******;Timezone=UTC;";
    builder.Services.AddDbContext<ErpDbContext>(options => options.UseNpgsql(connectionString));
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", b =>
    {
        b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
