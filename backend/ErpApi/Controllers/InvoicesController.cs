using ErpApi.Data;
using ErpApi.Authorization;
using ErpApi.DTOs.Companies;
using ErpApi.DTOs.Invoices;
using ErpApi.Models;
using ErpApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpApi.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize]
public class InvoicesController : ControllerBase
{
    [Authorize(Policy = PermissionConstants.InvoiceRead)]
    [HttpGet("stats")]
    public async Task<IActionResult> GetInvoiceStats([FromServices] ErpDbContext db)
    {
        var invoices = await db.Invoices.ToListAsync();

        var totalRevenue = invoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.GrandTotal);
        var pendingCount = invoices.Count(i => i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Draft);
        var overdueCount = invoices.Count(i => i.Status == InvoiceStatus.Overdue);
        var averageInvoiceValue = invoices.Count > 0 ? invoices.Average(i => i.GrandTotal) : 0;

        return Ok(new
        {
            totalRevenue,
            pendingCount,
            overdueCount,
            averageInvoiceValue,
            totalInvoices = invoices.Count,
        });
    }

    [Authorize(Policy = PermissionConstants.InvoiceRead)]
    [HttpGet]
    public async Task<IActionResult> GetAllInvoices(
        [FromServices] ErpDbContext db,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
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

        return Ok(new
        {
            items = invoices.Select(i => ToResponse(i, includeItems: false)),
            totalCount,
            page,
            pageSize,
        });
    }

    [Authorize(Policy = PermissionConstants.InvoiceRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetInvoiceById(Guid id, [FromServices] ErpDbContext db)
    {
        var invoice = await db.Invoices
            .Include(i => i.Company)
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.InvoiceId == id);

        return invoice == null ? NotFound() : Ok(ToResponse(invoice, includeItems: true));
    }

    [Authorize(Policy = PermissionConstants.InvoiceWrite)]
    [HttpPost]
    public async Task<IActionResult> CreateInvoice(
        [FromBody] InvoiceRequest request,
        [FromServices] ErpDbContext db,
        [FromServices] CurrentUserService currentUserService,
        [FromServices] AuditService auditService)
    {
        var validationError = await ValidateInvoiceRequest(request, db, null);
        if (validationError != null)
        {
            return BadRequest(new { message = validationError });
        }

        var items = BuildInvoiceItems(request.Items);

        var invoiceDate = DateTime.SpecifyKind(request.InvoiceDate, DateTimeKind.Utc);
        var dueDate = DateTime.SpecifyKind(request.DueDate, DateTimeKind.Utc);

        var invoice = new Invoice
        {
            InvoiceId = Guid.NewGuid(),
            InvoiceNumber = request.InvoiceNumber.Trim(),
            CompanyId = request.CompanyId,
            InvoiceDate = invoiceDate,
            DueDate = dueDate,
            Notes = request.Notes,
            Status = request.Status,
            CreatedBy = currentUserService.Username,
            UpdatedBy = currentUserService.Username,
            CreatedDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            UpdatedDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            Items = items,
            TotalAmount = items.Sum(i => i.Amount),
            TaxAmount = items.Sum(i => i.TaxAmount),
        };
        invoice.GrandTotal = invoice.TotalAmount + invoice.TaxAmount;

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(
            actionType: "invoice.create",
            entityName: nameof(Invoice),
            entityId: invoice.InvoiceId.ToString(),
            newValues: CreateSnapshot(invoice));

        return Created($"/api/invoices/{invoice.InvoiceId}", ToResponse(invoice, includeItems: true));
    }

    [Authorize(Policy = PermissionConstants.InvoiceWrite)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateInvoice(
        Guid id,
        [FromBody] InvoiceRequest request,
        [FromServices] ErpDbContext db,
        [FromServices] CurrentUserService currentUserService,
        [FromServices] AuditService auditService)
    {
        var invoice = await db.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.InvoiceId == id);

        if (invoice == null) return NotFound();

        var validationError = await ValidateInvoiceRequest(request, db, id);
        if (validationError != null)
        {
            return BadRequest(new { message = validationError });
        }

        var oldValues = CreateSnapshot(invoice);
        var items = BuildInvoiceItems(request.Items);
        foreach (var item in items)
        {
            item.InvoiceId = invoice.InvoiceId;
        }

        var invoiceDate = DateTime.SpecifyKind(request.InvoiceDate, DateTimeKind.Utc);
        var dueDate = DateTime.SpecifyKind(request.DueDate, DateTimeKind.Utc);

        invoice.InvoiceNumber = request.InvoiceNumber.Trim();
        invoice.CompanyId = request.CompanyId;
        invoice.InvoiceDate = invoiceDate;
        invoice.DueDate = dueDate;
        invoice.Notes = request.Notes;
        invoice.Status = request.Status;
        invoice.UpdatedBy = currentUserService.Username;
        invoice.UpdatedDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

        var existingItems = await db.InvoiceItems.Where(ii => ii.InvoiceId == invoice.InvoiceId).ToListAsync();
        db.InvoiceItems.RemoveRange(existingItems);
        db.InvoiceItems.AddRange(items);
        invoice.Items = items;

        invoice.TotalAmount = items.Sum(i => i.Amount);
        invoice.TaxAmount = items.Sum(i => i.TaxAmount);
        invoice.GrandTotal = invoice.TotalAmount + invoice.TaxAmount;

        await db.SaveChangesAsync();
        await auditService.WriteAsync(
            actionType: "invoice.update",
            entityName: nameof(Invoice),
            entityId: invoice.InvoiceId.ToString(),
            oldValues: oldValues,
            newValues: CreateSnapshot(invoice));

        return Ok(ToResponse(invoice, includeItems: true));
    }

    [Authorize(Policy = PermissionConstants.InvoiceDelete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteInvoice(
        Guid id,
        [FromServices] ErpDbContext db,
        [FromServices] AuditService auditService)
    {
        var invoice = await db.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.InvoiceId == id);

        if (invoice == null) return NotFound();

        var oldValues = CreateSnapshot(invoice);
        db.Invoices.Remove(invoice);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(
            actionType: "invoice.delete",
            entityName: nameof(Invoice),
            entityId: id.ToString(),
            oldValues: oldValues);

        return Ok(new { message = "Deleted" });
    }

    [Authorize(Policy = PermissionConstants.InvoiceWrite)]
    [HttpPost("{id:guid}/pdf")]
    public async Task<IActionResult> GenerateInvoicePdf(
        Guid id,
        [FromServices] ErpDbContext db,
        [FromServices] InvoicePdfService pdfService,
        [FromServices] AuditService auditService)
    {
        var invoice = await db.Invoices
            .Include(i => i.Company)
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.InvoiceId == id);

        if (invoice == null) return NotFound();

        var pdfBytes = pdfService.GenerateInvoicePdf(invoice, invoice.Company);
        await auditService.WriteAsync(
            actionType: "invoice.pdf.generate",
            entityName: nameof(Invoice),
            entityId: invoice.InvoiceId.ToString(),
            metadata: new { invoice.InvoiceNumber, invoice.CompanyId });

        return File(pdfBytes, "application/pdf", $"invoice-{invoice.InvoiceNumber}.pdf");
    }

    private async Task<string?> ValidateInvoiceRequest(InvoiceRequest request, ErpDbContext db, Guid? existingInvoiceId)
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

    private static List<InvoiceItem> BuildInvoiceItems(List<InvoiceItemRequest> requestItems)
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

    private static InvoiceResponse ToResponse(Invoice invoice, bool includeItems)
    {
        return new InvoiceResponse
        {
            InvoiceId = invoice.InvoiceId,
            InvoiceNumber = invoice.InvoiceNumber,
            CompanyId = invoice.CompanyId,
            Company = invoice.Company == null ? null : new CompanyResponse
            {
                CompanyId = invoice.Company.CompanyId,
                CompanyName = invoice.Company.CompanyName,
                Address = invoice.Company.Address,
                City = invoice.Company.City,
                State = invoice.Company.State,
                PinCode = invoice.Company.PinCode,
                Email = invoice.Company.Email,
                PhoneNumber = invoice.Company.PhoneNumber,
                Website = invoice.Company.Website,
                GSTIN = invoice.Company.GSTIN,
                PAN = invoice.Company.PAN,
                DrugLicenceNumber = invoice.Company.DrugLicenceNumber,
                UdogAadhaar = invoice.Company.UdogAadhaar,
                AadhaarNumber = invoice.Company.AadhaarNumber,
                MSMENumber = invoice.Company.MSMENumber,
                FSSAINumber = invoice.Company.FSSAINumber,
                CreatedBy = invoice.Company.CreatedBy,
                UpdatedBy = invoice.Company.UpdatedBy,
                CreatedDate = invoice.Company.CreatedDate,
                UpdatedDate = invoice.Company.UpdatedDate,
                IsActive = invoice.Company.IsActive,
            },
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            TotalAmount = invoice.TotalAmount,
            TaxAmount = invoice.TaxAmount,
            GrandTotal = invoice.GrandTotal,
            Status = invoice.Status,
            Notes = invoice.Notes,
            CreatedBy = invoice.CreatedBy,
            CreatedDate = invoice.CreatedDate,
            UpdatedBy = invoice.UpdatedBy,
            UpdatedDate = invoice.UpdatedDate,
            Items = includeItems
                ? invoice.Items.Select(item => new InvoiceItemResponse
                {
                    InvoiceItemId = item.InvoiceItemId,
                    InvoiceId = item.InvoiceId,
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Amount = item.Amount,
                    TaxRate = item.TaxRate,
                    TaxAmount = item.TaxAmount,
                    CreatedDate = item.CreatedDate,
                }).ToList()
                : new List<InvoiceItemResponse>(),
        };
    }

    private static object CreateSnapshot(Invoice invoice) => new
    {
        invoice.InvoiceId,
        invoice.InvoiceNumber,
        invoice.CompanyId,
        invoice.InvoiceDate,
        invoice.DueDate,
        invoice.TotalAmount,
        invoice.TaxAmount,
        invoice.GrandTotal,
        invoice.Status,
        invoice.Notes,
        invoice.CreatedBy,
        invoice.CreatedDate,
        invoice.UpdatedBy,
        invoice.UpdatedDate,
        Items = invoice.Items.Select(item => new
        {
            item.InvoiceItemId,
            item.InvoiceId,
            item.Description,
            item.Quantity,
            item.UnitPrice,
            item.Amount,
            item.TaxRate,
            item.TaxAmount,
            item.CreatedDate,
        }).ToList(),
    };
}
