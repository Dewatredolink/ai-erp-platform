using ErpApi.Data;
using ErpApi.Authorization;
using ErpApi.DTOs.Companies;
using ErpApi.Models;
using ErpApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpApi.Controllers;

[ApiController]
[Route("api/companies")]
[Authorize]
public class CompaniesController : ControllerBase
{
    [Authorize(Policy = PermissionConstants.CompanyRead)]
    [HttpGet]
    public async Task<IActionResult> GetAllCompanies([FromServices] ErpDbContext db)
    {
        var companies = await db.Companies.ToListAsync();
        return Ok(companies.Select(ToResponse));
    }

    [Authorize(Policy = PermissionConstants.CompanyRead)]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCompanyById(int id, [FromServices] ErpDbContext db)
    {
        var company = await db.Companies.FindAsync(id);
        return company == null ? NotFound() : Ok(ToResponse(company));
    }

    [Authorize(Policy = PermissionConstants.CompanyWrite)]
    [HttpPost]
    public async Task<IActionResult> CreateCompany(
        [FromBody] CompanyRequest request,
        [FromServices] ErpDbContext db,
        [FromServices] CurrentUserService currentUserService,
        [FromServices] AuditService auditService)
    {
        var username = currentUserService.Username;
        var company = new Company
        {
            CompanyName = request.CompanyName!,
            GSTIN = request.GSTIN!,
            PAN = request.PAN!,
            Address = request.Address!,
            City = request.City!,
            State = request.State!,
            PinCode = request.PinCode!,
            PhoneNumber = request.PhoneNumber!,
            Email = request.Email!,
            Website = request.Website!,
            DrugLicenceNumber = request.DrugLicenceNumber!,
            UdogAadhaar = request.UdogAadhaar!,
            AadhaarNumber = request.AadhaarNumber!,
            MSMENumber = request.MSMENumber!,
            FSSAINumber = request.FSSAINumber!,
            CreatedBy = username,
            UpdatedBy = username,
            UpdatedDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            IsActive = request.IsActive,
            CreatedDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
        };

        db.Companies.Add(company);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(
            actionType: "company.create",
            entityName: nameof(Company),
            entityId: company.CompanyId.ToString(),
            newValues: CreateSnapshot(company));

        return Created($"/api/companies/{company.CompanyId}", ToResponse(company));
    }

    [Authorize(Policy = PermissionConstants.CompanyWrite)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCompany(
        int id,
        [FromBody] CompanyRequest request,
        [FromServices] ErpDbContext db,
        [FromServices] CurrentUserService currentUserService,
        [FromServices] AuditService auditService)
    {
        var company = await db.Companies.FindAsync(id);
        if (company == null) return NotFound();

        var oldValues = CreateSnapshot(company);
        company.CompanyName = request.CompanyName ?? company.CompanyName;
        company.GSTIN = request.GSTIN ?? company.GSTIN;
        company.PAN = request.PAN ?? company.PAN;
        company.Address = request.Address ?? company.Address;
        company.City = request.City ?? company.City;
        company.State = request.State ?? company.State;
        company.PinCode = request.PinCode ?? company.PinCode;
        company.PhoneNumber = request.PhoneNumber ?? company.PhoneNumber;
        company.Email = request.Email ?? company.Email;
        company.Website = request.Website ?? company.Website;
        company.DrugLicenceNumber = request.DrugLicenceNumber ?? company.DrugLicenceNumber;
        company.UdogAadhaar = request.UdogAadhaar ?? company.UdogAadhaar;
        company.AadhaarNumber = request.AadhaarNumber ?? company.AadhaarNumber;
        company.MSMENumber = request.MSMENumber ?? company.MSMENumber;
        company.FSSAINumber = request.FSSAINumber ?? company.FSSAINumber;
        company.IsActive = request.IsActive;
        company.UpdatedBy = currentUserService.Username;
        company.UpdatedDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

        await db.SaveChangesAsync();
        await auditService.WriteAsync(
            actionType: "company.update",
            entityName: nameof(Company),
            entityId: company.CompanyId.ToString(),
            oldValues: oldValues,
            newValues: CreateSnapshot(company));

        return Ok(ToResponse(company));
    }

    [Authorize(Policy = PermissionConstants.CompanyDelete)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCompany(
        int id,
        [FromServices] ErpDbContext db,
        [FromServices] AuditService auditService)
    {
        var company = await db.Companies.FindAsync(id);
        if (company == null) return NotFound();

        var oldValues = CreateSnapshot(company);
        db.Companies.Remove(company);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(
            actionType: "company.delete",
            entityName: nameof(Company),
            entityId: id.ToString(),
            oldValues: oldValues);

        return Ok("Deleted");
    }

    private static CompanyResponse ToResponse(Company company) => new()
    {
        CompanyId = company.CompanyId,
        CompanyName = company.CompanyName,
        Address = company.Address,
        City = company.City,
        State = company.State,
        PinCode = company.PinCode,
        Email = company.Email,
        PhoneNumber = company.PhoneNumber,
        Website = company.Website,
        GSTIN = company.GSTIN,
        PAN = company.PAN,
        DrugLicenceNumber = company.DrugLicenceNumber,
        UdogAadhaar = company.UdogAadhaar,
        AadhaarNumber = company.AadhaarNumber,
        MSMENumber = company.MSMENumber,
        FSSAINumber = company.FSSAINumber,
        CreatedBy = company.CreatedBy,
        UpdatedBy = company.UpdatedBy,
        CreatedDate = company.CreatedDate,
        UpdatedDate = company.UpdatedDate,
        IsActive = company.IsActive,
    };

    private static object CreateSnapshot(Company company) => new
    {
        company.CompanyId,
        company.CompanyName,
        company.Address,
        company.City,
        company.State,
        company.PinCode,
        company.Email,
        company.PhoneNumber,
        company.Website,
        company.GSTIN,
        company.PAN,
        company.DrugLicenceNumber,
        company.UdogAadhaar,
        company.AadhaarNumber,
        company.MSMENumber,
        company.FSSAINumber,
        company.CreatedBy,
        company.UpdatedBy,
        company.CreatedDate,
        company.UpdatedDate,
        company.IsActive,
    };
}
