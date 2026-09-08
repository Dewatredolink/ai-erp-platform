using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ErpApi.Data;
using ErpApi.Models;
using ErpApi.Models.Auth;
using ErpApi.Services;

var builder = WebApplication.CreateBuilder(args);

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton<JwtTokenService>();

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
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

var connectionString = "Host=localhost;Port=5432;Database=erp_db;Username=postgres;Password=Dewa@2025";
builder.Services.AddDbContext<ErpDbContext>(options => options.UseNpgsql(connectionString));

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
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/auth/register", Register).WithName("Register");
app.MapPost("/api/auth/login", Login).WithName("Login");
app.MapPost("/api/auth/logout", Logout).WithName("Logout");
app.MapGet("/api/auth/me", GetCurrentUser).WithName("GetCurrentUser").RequireAuthorization();

app.MapGet("/api/companies", GetAllCompanies).WithName("GetCompanies");
app.MapGet("/api/companies/{id}", GetCompanyById).WithName("GetCompanyById");
app.MapPost("/api/companies", CreateCompany).WithName("CreateCompany");
app.MapPut("/api/companies/{id}", UpdateCompany).WithName("UpdateCompany");
app.MapDelete("/api/companies/{id}", DeleteCompany).WithName("DeleteCompany");

app.Run();

async Task<IResult> Register(RegisterRequest request, ErpDbContext db)
{
    if (string.IsNullOrWhiteSpace(request.Username) ||
        string.IsNullOrWhiteSpace(request.Email) ||
        string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { message = "Username, email and password are required." });
    }

    if (request.Password.Length < 8)
    {
        return Results.BadRequest(new { message = "Password must be at least 8 characters long." });
    }

    var normalizedUsername = request.Username.Trim();
    var normalizedEmail = request.Email.Trim().ToLowerInvariant();

    var existingUser = await db.Users.FirstOrDefaultAsync(u =>
        u.Username == normalizedUsername || u.Email == normalizedEmail);

    if (existingUser != null)
    {
        // Avoid revealing which field (username/email) already exists.
        return Results.Conflict(new { message = "Unable to register with the provided credentials." });
    }

    var user = new User
    {
        Username = normalizedUsername,
        Email = normalizedEmail,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        CreatedDate = DateTime.UtcNow,
        UpdatedDate = DateTime.UtcNow,
        IsActive = true,
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created($"/api/auth/me", new { user.UserId, user.Username, user.Email });
}

async Task<IResult> Login(LoginRequest request, ErpDbContext db, JwtTokenService tokenService)
{
    if (string.IsNullOrWhiteSpace(request.UsernameOrEmail) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { message = "Username/email and password are required." });
    }

    var normalizedIdentifier = request.UsernameOrEmail.Trim();
    var normalizedEmail = normalizedIdentifier.ToLowerInvariant();

    var user = await db.Users.FirstOrDefaultAsync(u =>
        u.Username == normalizedIdentifier || u.Email == normalizedEmail);

    if (user == null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
    {
        // Generic message so we don't reveal whether the account exists.
        return Results.Json(new { message = "Invalid username/email or password." }, statusCode: StatusCodes.Status401Unauthorized);
    }

    var (token, expiresAt) = tokenService.GenerateToken(user);

    return Results.Ok(new AuthResponse
    {
        UserId = user.UserId,
        Username = user.Username,
        Email = user.Email,
        Token = token,
        ExpiresAt = expiresAt,
    });
}

IResult Logout()
{
    // Token invalidation is handled client-side by discarding the JWT.
    // This endpoint exists so clients have a consistent logout call to make.
    return Results.Ok(new { message = "Logged out successfully." });
}

async Task<IResult> GetCurrentUser(ClaimsPrincipal principal, ErpDbContext db)
{
    var userIdClaim = principal.FindFirst("userId")?.Value;
    if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var user = await db.Users.FindAsync(userId);
    if (user == null)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new { user.UserId, user.Username, user.Email });
}

async Task<IResult> GetAllCompanies(ErpDbContext db)
{
    var companies = await db.Companies.ToListAsync();
    return Results.Ok(companies);
}

async Task<IResult> GetCompanyById(int id, ErpDbContext db)
{
    var company = await db.Companies.FindAsync(id);
    return company == null ? Results.NotFound() : Results.Ok(company);
}

async Task<IResult> CreateCompany(Company company, ErpDbContext db)
{
    company.CreatedDate = DateTime.UtcNow;
    db.Companies.Add(company);
    await db.SaveChangesAsync();
    return Results.Created($"/api/companies/{company.CompanyId}", company);
}

async Task<IResult> UpdateCompany(int id, Company updatedCompany, ErpDbContext db)
{
    var company = await db.Companies.FindAsync(id);
    if (company == null) return Results.NotFound();
    
    company.CompanyName = updatedCompany.CompanyName ?? company.CompanyName;
    company.GSTIN = updatedCompany.GSTIN ?? company.GSTIN;
    company.PAN = updatedCompany.PAN ?? company.PAN;
    company.Address = updatedCompany.Address ?? company.Address;
    company.City = updatedCompany.City ?? company.City;
    company.State = updatedCompany.State ?? company.State;
    company.PinCode = updatedCompany.PinCode ?? company.PinCode;
    company.PhoneNumber = updatedCompany.PhoneNumber ?? company.PhoneNumber;
    company.Email = updatedCompany.Email ?? company.Email;
    company.Website = updatedCompany.Website ?? company.Website;
    company.DrugLicenceNumber = updatedCompany.DrugLicenceNumber ?? company.DrugLicenceNumber;
    company.UdogAadhaar = updatedCompany.UdogAadhaar ?? company.UdogAadhaar;
    company.AadhaarNumber = updatedCompany.AadhaarNumber ?? company.AadhaarNumber;
    company.MSMENumber = updatedCompany.MSMENumber ?? company.MSMENumber;
    company.FSSAINumber = updatedCompany.FSSAINumber ?? company.FSSAINumber;
    company.IsActive = updatedCompany.IsActive;
    company.UpdatedDate = DateTime.UtcNow;
    
    await db.SaveChangesAsync();
    return Results.Ok(company);
}

async Task<IResult> DeleteCompany(int id, ErpDbContext db)
{
    var company = await db.Companies.FindAsync(id);
    if (company == null) return Results.NotFound();
    
    db.Companies.Remove(company);
    await db.SaveChangesAsync();
    return Results.Ok("Deleted");
}