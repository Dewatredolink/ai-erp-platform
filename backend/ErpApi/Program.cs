using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ErpApi.Data;
using ErpApi.Models;
using ErpApi.Models.Auth;
using ErpApi.Models.Invoicing;
using ErpApi.Services;

var builder = WebApplication.CreateBuilder(args);

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<InvoicePdfService>();

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
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

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

// Invoice routes - ORDER MATTERS! Put specific routes BEFORE general ones
app.MapGet("/api/invoices/stats", GetInvoiceStats).WithName("GetInvoiceStats");
app.MapGet("/api/invoices", GetAllInvoices).WithName("GetInvoices");
app.MapGet("/api/invoices/{id}", GetInvoiceById).WithName("GetInvoiceById");
app.MapPost("/api/invoices", CreateInvoice).WithName("CreateInvoice");
app.MapPut("/api/invoices/{id}", UpdateInvoice).WithName("UpdateInvoice");
app.MapDelete("/api/invoices/{id}", DeleteInvoice).WithName("DeleteInvoice");
app.MapPost("/api/invoices/{id}/pdf", GenerateInvoicePdf).WithName("GenerateInvoicePdf");

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
        CreatedDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
        UpdatedDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
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
    company.CreatedDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
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
    company.UpdatedDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
    
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

async Task<string?> ValidateInvoiceRequest(InvoiceRequest request, ErpDbContext db, Guid? existingInvoiceId)
{
    if (string.IsNullOrWhiteSpace(request.InvoiceNumber))
    {
        return "Invoice number is required.";
    }

    if (request.Items == null || request.Items.Count == 0)
    {
        return "At least one invoice item is required.";
    }

    if (request.DueDate < request.InvoiceDate)
    {
        return "Due date must be on or after the invoice date.";
    }

    var companyExists = await db.Companies.AnyAsync(c => c.CompanyId == request.CompanyId);
    if (!companyExists)
    {
        return "Company does not exist.";
    }

    var normalizedNumber = request.InvoiceNumber.Trim();
    var duplicateQuery = db.Invoices.Where(i => i.InvoiceNumber == normalizedNumber);
    if (existingInvoiceId.HasValue)
    {
        duplicateQuery = duplicateQuery.Where(i => i.InvoiceId != existingInvoiceId.Value);
    }

    if (await duplicateQuery.AnyAsync())
    {
        return "Invoice number must be unique.";
    }

    foreach (var item in request.Items)
    {
        if (string.IsNullOrWhiteSpace(item.Description))
        {
            return "Each invoice item must have a description.";
        }

        if (item.Quantity <= 0)
        {
            return "Item quantity must be greater than zero.";
        }

        if (item.UnitPrice < 0)
        {
            return "Item unit price cannot be negative.";
        }
    }

    return null;
}

List<InvoiceItem> BuildInvoiceItems(List<InvoiceItemRequest> requestItems)
{
    return requestItems.Select(item =>
    {
        var amount = Math.Round(item.Quantity * item.UnitPrice, 2);
        var taxAmount = Math.Round(amount * (item.TaxRate ?? 0) / 100, 2);
        return new InvoiceItem
        {
            InvoiceItemId = Guid.NewGuid(),
            Description = item.Description.Trim(),
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            Amount = amount,
            TaxRate = item.TaxRate,
            TaxAmount = taxAmount,
            CreatedDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
        };
    }).ToList();
}

async Task<IResult> GetAllInvoices(
    ErpDbContext db,
    int page = 1,
    int pageSize = 20,
    string? status = null,
    string? search = null,
    DateTime? fromDate = null,
    DateTime? toDate = null)
{
    if (page < 1) page = 1;
    if (pageSize < 1 || pageSize > 100) pageSize = 20;

    var query = db.Invoices.Include(i => i.Company).AsQueryable();

    if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<InvoiceStatus>(status, true, out var statusFilter))
    {
        query = query.Where(i => i.Status == statusFilter);
    }

    if (!string.IsNullOrWhiteSpace(search))
    {
        var term = search.Trim();
        query = query.Where(i => EF.Functions.ILike(i.InvoiceNumber, $"%{term}%"));
    }

    if (fromDate.HasValue)
    {
        query = query.Where(i => i.InvoiceDate >= fromDate.Value);
    }

    if (toDate.HasValue)
    {
        query = query.Where(i => i.InvoiceDate <= toDate.Value);
    }

    var totalCount = await query.CountAsync();

    var invoices = await query
        .OrderByDescending(i => i.InvoiceDate)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return Results.Ok(new
    {
        items = invoices,
        totalCount,
        page,
        pageSize,
    });
}

async Task<IResult> GetInvoiceStats(ErpDbContext db)
{
    var invoices = await db.Invoices.ToListAsync();

    var totalRevenue = invoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.GrandTotal);
    var pendingCount = invoices.Count(i => i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Draft);
    var overdueCount = invoices.Count(i => i.Status == InvoiceStatus.Overdue);
    var averageInvoiceValue = invoices.Count > 0 ? invoices.Average(i => i.GrandTotal) : 0;

    return Results.Ok(new
    {
        totalRevenue,
        pendingCount,
        overdueCount,
        averageInvoiceValue,
        totalInvoices = invoices.Count,
    });
}

async Task<IResult> GetInvoiceById(Guid id, ErpDbContext db)
{
    var invoice = await db.Invoices
        .Include(i => i.Company)
        .Include(i => i.Items)
        .FirstOrDefaultAsync(i => i.InvoiceId == id);

    return invoice == null ? Results.NotFound() : Results.Ok(invoice);
}

async Task<IResult> CreateInvoice(InvoiceRequest request, ErpDbContext db)
{
    var validationError = await ValidateInvoiceRequest(request, db, null);
    if (validationError != null)
    {
        return Results.BadRequest(new { message = validationError });
    }

    var items = BuildInvoiceItems(request.Items);

    // Parse dates properly for PostgreSQL
    var invoiceDate = DateTime.SpecifyKind(DateTime.Parse(request.InvoiceDate), DateTimeKind.Utc);
    var dueDate = DateTime.SpecifyKind(DateTime.Parse(request.DueDate), DateTimeKind.Utc);

    var invoice = new Invoice
    {
        InvoiceId = Guid.NewGuid(),
        InvoiceNumber = request.InvoiceNumber.Trim(),
        CompanyId = request.CompanyId,
        InvoiceDate = invoiceDate,
        DueDate = dueDate,
        Notes = request.Notes,
        Status = request.Status,
        CreatedBy = string.IsNullOrWhiteSpace(request.CreatedBy) ? "Admin" : request.CreatedBy,
        UpdatedBy = string.IsNullOrWhiteSpace(request.CreatedBy) ? "Admin" : request.CreatedBy,
        CreatedDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
        UpdatedDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
        Items = items,
        TotalAmount = items.Sum(i => i.Amount),
        TaxAmount = items.Sum(i => i.TaxAmount),
    };
    invoice.GrandTotal = invoice.TotalAmount + invoice.TaxAmount;

    db.Invoices.Add(invoice);
    await db.SaveChangesAsync();

    return Results.Created($"/api/invoices/{invoice.InvoiceId}", invoice);
}

async Task<IResult> UpdateInvoice(Guid id, InvoiceRequest request, ErpDbContext db)
{
    var invoice = await db.Invoices
        .Include(i => i.Items)
        .FirstOrDefaultAsync(i => i.InvoiceId == id);

    if (invoice == null) return Results.NotFound();

    var validationError = await ValidateInvoiceRequest(request, db, id);
    if (validationError != null)
    {
        return Results.BadRequest(new { message = validationError });
    }

    var items = BuildInvoiceItems(request.Items);
    foreach (var item in items)
    {
        item.InvoiceId = invoice.InvoiceId;
    }

    // Parse dates properly for PostgreSQL
    var invoiceDate = DateTime.SpecifyKind(DateTime.Parse(request.InvoiceDate), DateTimeKind.Utc);
    var dueDate = DateTime.SpecifyKind(DateTime.Parse(request.DueDate), DateTimeKind.Utc);

    invoice.InvoiceNumber = request.InvoiceNumber.Trim();
    invoice.CompanyId = request.CompanyId;
    invoice.InvoiceDate = invoiceDate;
    invoice.DueDate = dueDate;
    invoice.Notes = request.Notes;
    invoice.Status = request.Status;
    invoice.UpdatedBy = string.IsNullOrWhiteSpace(request.CreatedBy) ? invoice.UpdatedBy : request.CreatedBy;
    invoice.UpdatedDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

    var existingItems = await db.InvoiceItems.Where(ii => ii.InvoiceId == invoice.InvoiceId).ToListAsync();
    db.InvoiceItems.RemoveRange(existingItems);
    db.InvoiceItems.AddRange(items);
    invoice.Items = items;

    invoice.TotalAmount = items.Sum(i => i.Amount);
    invoice.TaxAmount = items.Sum(i => i.TaxAmount);
    invoice.GrandTotal = invoice.TotalAmount + invoice.TaxAmount;

    await db.SaveChangesAsync();

    return Results.Ok(invoice);
}

async Task<IResult> DeleteInvoice(Guid id, ErpDbContext db)
{
    var invoice = await db.Invoices.FindAsync(id);
    if (invoice == null) return Results.NotFound();

    db.Invoices.Remove(invoice);
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Deleted" });
}

async Task<IResult> GenerateInvoicePdf(Guid id, ErpDbContext db, InvoicePdfService pdfService)
{
    var invoice = await db.Invoices
        .Include(i => i.Company)
        .Include(i => i.Items)
        .FirstOrDefaultAsync(i => i.InvoiceId == id);

    if (invoice == null) return Results.NotFound();

    var pdfBytes = pdfService.GenerateInvoicePdf(invoice, invoice.Company);
    return Results.File(pdfBytes, "application/pdf", $"invoice-{invoice.InvoiceNumber}.pdf");
}
