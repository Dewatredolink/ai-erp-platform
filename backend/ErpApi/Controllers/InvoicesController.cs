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
    public async Task<IActionResult> GetInvoiceStats(
        [FromServices] ErpDbContext db,
        [FromServices] CurrentUserScopeService currentUserScopeService)
    {
        var scope = await currentUserScopeService.GetScopeAsync();
        var invoices = await currentUserScopeService
            .ApplyInvoiceScope(db.Invoices.AsNoTracking(), scope)
            .ToListAsync();

        var totalRevenue = invoices.Where(i => i.ApprovalStatus == InvoiceApprovalStatus.Paid || i.Status == InvoiceStatus.Paid).Sum(i => i.GrandTotal);
        var pendingCount = invoices.Count(i =>
            i.ApprovalStatus == InvoiceApprovalStatus.Draft
            || i.ApprovalStatus == InvoiceApprovalStatus.Submitted
            || i.ApprovalStatus == InvoiceApprovalStatus.Approved);
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
        [FromServices] CurrentUserScopeService currentUserScopeService,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var scope = await currentUserScopeService.GetScopeAsync();
        var query = currentUserScopeService.ApplyInvoiceScope(
                db.Invoices
                    .Include(i => i.Company)
                    .ThenInclude(c => c!.Branch)
                    .AsNoTracking(),
                scope)
            .AsQueryable();

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
    public async Task<IActionResult> GetInvoiceById(
        Guid id,
        [FromServices] ErpDbContext db,
        [FromServices] CurrentUserScopeService currentUserScopeService)
    {
        var scope = await currentUserScopeService.GetScopeAsync();
        var invoice = await currentUserScopeService
            .ApplyInvoiceScope(db.Invoices
            .Include(i => i.Company)
            .ThenInclude(c => c!.Branch)
            .Include(i => i.Items)
            , scope)
            .FirstOrDefaultAsync(i => i.InvoiceId == id);

        return invoice == null ? NotFound() : Ok(ToResponse(invoice, includeItems: true));
    }

    [Authorize(Policy = PermissionConstants.InvoiceWrite)]
    [HttpPost]
    public async Task<IActionResult> CreateInvoice(
        [FromBody] InvoiceRequest request,
        [FromServices] ErpDbContext db,
        [FromServices] CurrentUserScopeService currentUserScopeService,
        [FromServices] CurrentUserService currentUserService,
        [FromServices] AuditService auditService)
    {
        var scope = await currentUserScopeService.GetScopeAsync();
        var companyAccessResult = await EnsureCompanyAccessAsync(request.CompanyId, db, currentUserScopeService, scope);
        if (companyAccessResult != null)
        {
            return companyAccessResult;
        }

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
            ApprovalStatus = InvoiceApprovalStatus.Draft,
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
        [FromServices] CurrentUserScopeService currentUserScopeService,
        [FromServices] CurrentUserService currentUserService,
        [FromServices] AuditService auditService)
    {
        var scope = await currentUserScopeService.GetScopeAsync();
        var invoice = await currentUserScopeService
            .ApplyInvoiceScope(db.Invoices
            .Include(i => i.Items)
            , scope)
            .FirstOrDefaultAsync(i => i.InvoiceId == id);

        if (invoice == null) return NotFound();

        if (!CanEditInvoice(invoice))
        {
            return Conflict(new { message = $"Invoice cannot be edited while in '{invoice.ApprovalStatus}' state." });
        }

        var companyAccessResult = await EnsureCompanyAccessAsync(request.CompanyId, db, currentUserScopeService, scope);
        if (companyAccessResult != null)
        {
            return companyAccessResult;
        }

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
        [FromServices] CurrentUserScopeService currentUserScopeService,
        [FromServices] AuditService auditService)
    {
        var scope = await currentUserScopeService.GetScopeAsync();
        var invoice = await currentUserScopeService
            .ApplyInvoiceScope(db.Invoices
            .Include(i => i.Items)
            , scope)
            .FirstOrDefaultAsync(i => i.InvoiceId == id);

        if (invoice == null) return NotFound();

        if (!CanDeleteInvoice(invoice))
        {
            return Conflict(new { message = $"Invoice cannot be deleted while in '{invoice.ApprovalStatus}' state." });
        }

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
        [FromServices] CurrentUserScopeService currentUserScopeService,
        [FromServices] InvoicePdfService pdfService,
        [FromServices] AuditService auditService)
    {
        var scope = await currentUserScopeService.GetScopeAsync();
        var invoice = await currentUserScopeService
            .ApplyInvoiceScope(db.Invoices
            .Include(i => i.Company)
            .ThenInclude(c => c!.Branch)
            .Include(i => i.Items)
            , scope)
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

    [Authorize(Policy = PermissionConstants.InvoiceSubmit)]
    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> SubmitInvoice(
        Guid id,
        [FromBody] InvoiceApprovalActionRequest? request,
        [FromServices] ErpDbContext db,
        [FromServices] CurrentUserScopeService currentUserScopeService,
        [FromServices] CurrentUserService currentUserService,
        [FromServices] AuditService auditService)
    {
        var scope = await currentUserScopeService.GetScopeAsync();
        var invoice = await currentUserScopeService
            .ApplyInvoiceScope(db.Invoices.Include(i => i.Items), scope)
            .FirstOrDefaultAsync(i => i.InvoiceId == id);
        if (invoice == null) return NotFound();

        if (invoice.ApprovalStatus != InvoiceApprovalStatus.Draft && invoice.ApprovalStatus != InvoiceApprovalStatus.Rejected)
        {
            return BadRequest(new { message = $"Invoice in '{invoice.ApprovalStatus}' state cannot be submitted." });
        }

        var fromStatus = invoice.ApprovalStatus;
        var oldValues = CreateSnapshot(invoice);
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
        invoice.ApprovalStatus = InvoiceApprovalStatus.Submitted;
        invoice.Status = InvoiceStatus.Sent;
        invoice.SubmittedByUserId = currentUserService.UserId;
        invoice.SubmittedBy = currentUserService.Username;
        invoice.SubmittedAt = now;
        invoice.ApprovalRemarks = request?.Remarks?.Trim();
        invoice.UpdatedBy = currentUserService.Username;
        invoice.UpdatedDate = now;

        await db.SaveChangesAsync();
        await auditService.WriteAsync(
            actionType: "invoice.submit",
            entityName: nameof(Invoice),
            entityId: invoice.InvoiceId.ToString(),
            oldValues: oldValues,
            newValues: CreateSnapshot(invoice),
            metadata: new
            {
                from = fromStatus.ToString(),
                to = invoice.ApprovalStatus.ToString(),
                remarks = invoice.ApprovalRemarks,
            });

        return Ok(ToResponse(invoice, includeItems: true));
    }

    [Authorize(Policy = PermissionConstants.InvoiceApprove)]
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> ApproveInvoice(
        Guid id,
        [FromBody] InvoiceApprovalActionRequest? request,
        [FromServices] ErpDbContext db,
        [FromServices] CurrentUserScopeService currentUserScopeService,
        [FromServices] CurrentUserService currentUserService,
        [FromServices] AuditService auditService)
    {
        var scope = await currentUserScopeService.GetScopeAsync();
        var invoice = await currentUserScopeService
            .ApplyInvoiceScope(db.Invoices.Include(i => i.Items), scope)
            .FirstOrDefaultAsync(i => i.InvoiceId == id);
        if (invoice == null) return NotFound();

        if (invoice.ApprovalStatus != InvoiceApprovalStatus.Submitted)
        {
            return BadRequest(new { message = $"Invoice in '{invoice.ApprovalStatus}' state cannot be approved." });
        }

        var fromStatus = invoice.ApprovalStatus;
        var oldValues = CreateSnapshot(invoice);
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
        invoice.ApprovalStatus = InvoiceApprovalStatus.Approved;
        invoice.Status = InvoiceStatus.Sent;
        invoice.ApprovedByUserId = currentUserService.UserId;
        invoice.ApprovedBy = currentUserService.Username;
        invoice.ApprovedAt = now;
        invoice.ApprovalRemarks = request?.Remarks?.Trim();
        invoice.UpdatedBy = currentUserService.Username;
        invoice.UpdatedDate = now;

        await db.SaveChangesAsync();
        await auditService.WriteAsync(
            actionType: "invoice.approve",
            entityName: nameof(Invoice),
            entityId: invoice.InvoiceId.ToString(),
            oldValues: oldValues,
            newValues: CreateSnapshot(invoice),
            metadata: new
            {
                from = fromStatus.ToString(),
                to = invoice.ApprovalStatus.ToString(),
                remarks = invoice.ApprovalRemarks,
            });

        return Ok(ToResponse(invoice, includeItems: true));
    }

    [Authorize(Policy = PermissionConstants.InvoiceReject)]
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> RejectInvoice(
        Guid id,
        [FromBody] InvoiceApprovalActionRequest? request,
        [FromServices] ErpDbContext db,
        [FromServices] CurrentUserScopeService currentUserScopeService,
        [FromServices] CurrentUserService currentUserService,
        [FromServices] AuditService auditService)
    {
        var scope = await currentUserScopeService.GetScopeAsync();
        var invoice = await currentUserScopeService
            .ApplyInvoiceScope(db.Invoices.Include(i => i.Items), scope)
            .FirstOrDefaultAsync(i => i.InvoiceId == id);
        if (invoice == null) return NotFound();

        if (invoice.ApprovalStatus != InvoiceApprovalStatus.Submitted)
        {
            return BadRequest(new { message = $"Invoice in '{invoice.ApprovalStatus}' state cannot be rejected." });
        }

        var fromStatus = invoice.ApprovalStatus;
        var oldValues = CreateSnapshot(invoice);
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
        invoice.ApprovalStatus = InvoiceApprovalStatus.Rejected;
        invoice.Status = InvoiceStatus.Draft;
        invoice.RejectedByUserId = currentUserService.UserId;
        invoice.RejectedBy = currentUserService.Username;
        invoice.RejectedAt = now;
        invoice.ApprovalRemarks = request?.Remarks?.Trim();
        invoice.UpdatedBy = currentUserService.Username;
        invoice.UpdatedDate = now;

        await db.SaveChangesAsync();
        await auditService.WriteAsync(
            actionType: "invoice.reject",
            entityName: nameof(Invoice),
            entityId: invoice.InvoiceId.ToString(),
            oldValues: oldValues,
            newValues: CreateSnapshot(invoice),
            metadata: new
            {
                from = fromStatus.ToString(),
                to = invoice.ApprovalStatus.ToString(),
                remarks = invoice.ApprovalRemarks,
            });

        return Ok(ToResponse(invoice, includeItems: true));
    }

    [Authorize(Policy = PermissionConstants.InvoicePay)]
    [HttpPost("{id:guid}/pay")]
    public async Task<IActionResult> MarkInvoicePaid(
        Guid id,
        [FromBody] InvoiceApprovalActionRequest? request,
        [FromServices] ErpDbContext db,
        [FromServices] CurrentUserScopeService currentUserScopeService,
        [FromServices] CurrentUserService currentUserService,
        [FromServices] AuditService auditService)
    {
        var scope = await currentUserScopeService.GetScopeAsync();
        var invoice = await currentUserScopeService
            .ApplyInvoiceScope(db.Invoices.Include(i => i.Items), scope)
            .FirstOrDefaultAsync(i => i.InvoiceId == id);
        if (invoice == null) return NotFound();

        if (invoice.ApprovalStatus != InvoiceApprovalStatus.Approved)
        {
            return BadRequest(new { message = $"Invoice in '{invoice.ApprovalStatus}' state cannot be marked as paid." });
        }

        var fromStatus = invoice.ApprovalStatus;
        var oldValues = CreateSnapshot(invoice);
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
        invoice.ApprovalStatus = InvoiceApprovalStatus.Paid;
        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidByUserId = currentUserService.UserId;
        invoice.PaidBy = currentUserService.Username;
        invoice.PaidAt = now;
        invoice.ApprovalRemarks = request?.Remarks?.Trim();
        invoice.UpdatedBy = currentUserService.Username;
        invoice.UpdatedDate = now;

        await db.SaveChangesAsync();
        await auditService.WriteAsync(
            actionType: "invoice.pay",
            entityName: nameof(Invoice),
            entityId: invoice.InvoiceId.ToString(),
            oldValues: oldValues,
            newValues: CreateSnapshot(invoice),
            metadata: new
            {
                from = fromStatus.ToString(),
                to = invoice.ApprovalStatus.ToString(),
                remarks = invoice.ApprovalRemarks,
            });

        return Ok(ToResponse(invoice, includeItems: true));
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
                BranchId = invoice.Company.BranchId,
                BranchName = invoice.Company.Branch?.Name,
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
            ApprovalStatus = invoice.ApprovalStatus,
            SubmittedByUserId = invoice.SubmittedByUserId,
            SubmittedBy = invoice.SubmittedBy,
            SubmittedAt = invoice.SubmittedAt,
            ApprovedByUserId = invoice.ApprovedByUserId,
            ApprovedBy = invoice.ApprovedBy,
            ApprovedAt = invoice.ApprovedAt,
            RejectedByUserId = invoice.RejectedByUserId,
            RejectedBy = invoice.RejectedBy,
            RejectedAt = invoice.RejectedAt,
            PaidByUserId = invoice.PaidByUserId,
            PaidBy = invoice.PaidBy,
            PaidAt = invoice.PaidAt,
            ApprovalRemarks = invoice.ApprovalRemarks,
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
        invoice.ApprovalStatus,
        invoice.SubmittedByUserId,
        invoice.SubmittedBy,
        invoice.SubmittedAt,
        invoice.ApprovedByUserId,
        invoice.ApprovedBy,
        invoice.ApprovedAt,
        invoice.RejectedByUserId,
        invoice.RejectedBy,
        invoice.RejectedAt,
        invoice.PaidByUserId,
        invoice.PaidBy,
        invoice.PaidAt,
        invoice.ApprovalRemarks,
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

    private static bool CanEditInvoice(Invoice invoice) =>
        invoice.ApprovalStatus == InvoiceApprovalStatus.Draft
        || invoice.ApprovalStatus == InvoiceApprovalStatus.Rejected;

    private static bool CanDeleteInvoice(Invoice invoice) => CanEditInvoice(invoice);

    private static async Task<IActionResult?> EnsureCompanyAccessAsync(
        int companyId,
        ErpDbContext db,
        CurrentUserScopeService currentUserScopeService,
        CurrentUserScope scope)
    {
        var hasAccess = await currentUserScopeService
            .ApplyCompanyScope(db.Companies.AsNoTracking(), scope)
            .AnyAsync(company => company.CompanyId == companyId);

        if (hasAccess)
        {
            return null;
        }

        var companyExists = await db.Companies.AsNoTracking().AnyAsync(company => company.CompanyId == companyId);
        return companyExists
            ? new ObjectResult(new { message = "You are not authorized to access the requested company." })
            {
                StatusCode = StatusCodes.Status403Forbidden,
            }
            : new BadRequestObjectResult(new { message = "Company does not exist." });
    }
}
