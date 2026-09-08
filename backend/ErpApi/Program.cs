using Microsoft.EntityFrameworkCore;
using ErpApi.Data;
using ErpApi.Models;

var builder = WebApplication.CreateBuilder(args);

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
app.UseAuthorization();

app.MapGet("/api/companies", GetAllCompanies).WithName("GetCompanies");
app.MapGet("/api/companies/{id}", GetCompanyById).WithName("GetCompanyById");
app.MapPost("/api/companies", CreateCompany).WithName("CreateCompany");
app.MapPut("/api/companies/{id}", UpdateCompany).WithName("UpdateCompany");
app.MapDelete("/api/companies/{id}", DeleteCompany).WithName("DeleteCompany");

app.Run();

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