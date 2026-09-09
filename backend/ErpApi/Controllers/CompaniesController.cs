using ErpApi.Data;
using ErpApi.DTOs.Companies;
using ErpApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpApi.Controllers;

[ApiController]
[Route("api/companies")]
public class CompaniesController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllCompanies([FromServices] ErpDbContext db)
    {
        var companies = await db.Companies.ToListAsync();
        return Ok(companies.Select(ToResponse));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCompanyById(int id, [FromServices] ErpDbContext db)
    {
        var company = await db.Companies.FindAsync(id);
        return company == null ? NotFound() : Ok(ToResponse(company));
    }

    [HttpPost]
    public async Task<IActionResult> CreateCompany([FromBody] CompanyRequest request, [FromServices] ErpDbContext db)
    {
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
            CreatedBy = request.CreatedBy!,
            UpdatedBy = request.UpdatedBy!,
            UpdatedDate = request.UpdatedDate,
            IsActive = request.IsActive,
            CreatedDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
        };

        db.Companies.Add(company);
        await db.SaveChangesAsync();
        return Created($"/api/companies/{company.CompanyId}", ToResponse(company));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCompany(int id, [FromBody] CompanyRequest request, [FromServices] ErpDbContext db)
    {
        var company = await db.Companies.FindAsync(id);
        if (company == null) return NotFound();

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
        company.UpdatedDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

        await db.SaveChangesAsync();
        return Ok(ToResponse(company));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCompany(int id, [FromServices] ErpDbContext db)
    {
        var company = await db.Companies.FindAsync(id);
        if (company == null) return NotFound();

        db.Companies.Remove(company);
        await db.SaveChangesAsync();
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
}
