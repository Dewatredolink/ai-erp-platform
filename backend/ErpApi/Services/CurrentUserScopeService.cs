using ErpApi.Authorization;
using ErpApi.Data;
using ErpApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ErpApi.Services;

public sealed class BranchAuthorizationResult
{
    public int? BranchId { get; init; }
    public Branch? Branch { get; init; }
    public int? StatusCode { get; init; }
    public string? Message { get; init; }
    public bool IsAuthorized => StatusCode is null;
}

public sealed class CurrentUserScope
{
    public bool HasGlobalAccess { get; init; }
    public IReadOnlyCollection<int> AllowedBranchIds { get; init; } = Array.Empty<int>();
    public IReadOnlyCollection<int> AllowedCompanyIds { get; init; } = Array.Empty<int>();
}

public class CurrentUserScopeService
{
    private readonly ErpDbContext _db;
    private readonly CurrentUserService _currentUserService;

    public CurrentUserScopeService(ErpDbContext db, CurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<CurrentUserScope> GetScopeAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return new CurrentUserScope();
        }

        var userId = _currentUserService.UserId.Value;
        var hasGlobalAccess = await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .AnyAsync(ur => ur.Role!.Name == RoleConstants.Administrator, cancellationToken);

        if (hasGlobalAccess)
        {
            return new CurrentUserScope { HasGlobalAccess = true };
        }

        var branchIds = await _db.UserBranches
            .Where(ub => ub.UserId == userId)
            .Select(ub => ub.BranchId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        var companyIds = await _db.UserCompanies
            .Where(uc => uc.UserId == userId)
            .Select(uc => uc.CompanyId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (branchIds.Length > 0)
        {
            var assignedBranchIds = branchIds.ToList();
            companyIds.AddRange(await _db.Companies
                .Where(company => company.BranchId.HasValue && assignedBranchIds.Contains(company.BranchId.Value))
                .Select(company => company.CompanyId)
                .Distinct()
                .ToListAsync(cancellationToken));
        }

        return new CurrentUserScope
        {
            AllowedBranchIds = branchIds,
            AllowedCompanyIds = companyIds.Distinct().ToArray(),
        };
    }

    public IQueryable<Company> ApplyCompanyScope(IQueryable<Company> query, CurrentUserScope scope)
    {
        if (scope.HasGlobalAccess)
        {
            return query;
        }

        var companyIds = scope.AllowedCompanyIds.ToList();
        return companyIds.Count == 0
            ? query.Where(_ => false)
            : query.Where(company => companyIds.Contains(company.CompanyId));
    }

    public IQueryable<Invoice> ApplyInvoiceScope(IQueryable<Invoice> query, CurrentUserScope scope)
    {
        if (scope.HasGlobalAccess)
        {
            return query;
        }

        var companyIds = scope.AllowedCompanyIds.ToList();
        return companyIds.Count == 0
            ? query.Where(_ => false)
            : query.Where(invoice => companyIds.Contains(invoice.CompanyId));
    }

    public async Task<Company?> FindAccessibleCompanyAsync(
        IQueryable<Company> query,
        int companyId,
        CurrentUserScope? scope = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveScope = scope ?? await GetScopeAsync(cancellationToken);
        return await ApplyCompanyScope(query, effectiveScope)
            .FirstOrDefaultAsync(company => company.CompanyId == companyId, cancellationToken);
    }

    public async Task<Invoice?> FindAccessibleInvoiceAsync(
        IQueryable<Invoice> query,
        Guid invoiceId,
        CurrentUserScope? scope = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveScope = scope ?? await GetScopeAsync(cancellationToken);
        return await ApplyInvoiceScope(query, effectiveScope)
            .FirstOrDefaultAsync(invoice => invoice.InvoiceId == invoiceId, cancellationToken);
    }

    public async Task<(bool HasAccess, bool Exists)> CheckCompanyAccessAsync(
        int companyId,
        CurrentUserScope? scope = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveScope = scope ?? await GetScopeAsync(cancellationToken);
        var hasAccess = await ApplyCompanyScope(_db.Companies.AsNoTracking(), effectiveScope)
            .AnyAsync(company => company.CompanyId == companyId, cancellationToken);
        if (hasAccess)
        {
            return (true, true);
        }

        var exists = await _db.Companies.AsNoTracking()
            .AnyAsync(company => company.CompanyId == companyId, cancellationToken);
        return (false, exists);
    }

    public async Task<BranchAuthorizationResult> ResolveBranchForCompanyCreateAsync(
        int? branchId,
        CurrentUserScope? scope = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveScope = scope ?? await GetScopeAsync(cancellationToken);
        Branch? branch = null;
        var resolvedBranchId = branchId;

        if (resolvedBranchId.HasValue)
        {
            branch = await _db.Branches.FirstOrDefaultAsync(
                candidate => candidate.BranchId == resolvedBranchId.Value,
                cancellationToken);

            if (branch == null)
            {
                return new BranchAuthorizationResult
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Branch does not exist.",
                };
            }
        }

        if (!effectiveScope.HasGlobalAccess)
        {
            if (resolvedBranchId.HasValue)
            {
                if (!effectiveScope.AllowedBranchIds.Contains(resolvedBranchId.Value))
                {
                    return new BranchAuthorizationResult
                    {
                        StatusCode = StatusCodes.Status403Forbidden,
                        Message = "You are not authorized to access the requested branch.",
                    };
                }
            }
            else if (effectiveScope.AllowedBranchIds.Count == 1)
            {
                resolvedBranchId = effectiveScope.AllowedBranchIds.Single();
                branch = await _db.Branches.FirstOrDefaultAsync(
                    candidate => candidate.BranchId == resolvedBranchId.Value,
                    cancellationToken);
            }
            else if (effectiveScope.AllowedBranchIds.Count == 0)
            {
                return new BranchAuthorizationResult
                {
                    StatusCode = StatusCodes.Status403Forbidden,
                    Message = "You do not have an assigned branch for company creation.",
                };
            }
            else
            {
                return new BranchAuthorizationResult
                {
                    StatusCode = StatusCodes.Status403Forbidden,
                    Message = "BranchId is required when multiple branch assignments exist.",
                };
            }
        }

        return new BranchAuthorizationResult
        {
            BranchId = resolvedBranchId,
            Branch = branch,
        };
    }

    public async Task<BranchAuthorizationResult> ResolveBranchForCompanyUpdateAsync(
        int? requestedBranchId,
        int? currentBranchId,
        CurrentUserScope? scope = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveScope = scope ?? await GetScopeAsync(cancellationToken);
        if (requestedBranchId == currentBranchId)
        {
            return new BranchAuthorizationResult
            {
                BranchId = currentBranchId,
            };
        }

        if (requestedBranchId.HasValue)
        {
            var branch = await _db.Branches.FirstOrDefaultAsync(
                candidate => candidate.BranchId == requestedBranchId.Value,
                cancellationToken);
            if (branch == null)
            {
                return new BranchAuthorizationResult
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Branch does not exist.",
                };
            }

            if (!effectiveScope.HasGlobalAccess && !effectiveScope.AllowedBranchIds.Contains(requestedBranchId.Value))
            {
                return new BranchAuthorizationResult
                {
                    StatusCode = StatusCodes.Status403Forbidden,
                    Message = "You are not authorized to assign the requested branch.",
                };
            }

            return new BranchAuthorizationResult
            {
                BranchId = requestedBranchId,
                Branch = branch,
            };
        }

        if (!effectiveScope.HasGlobalAccess)
        {
            return new BranchAuthorizationResult
            {
                StatusCode = StatusCodes.Status403Forbidden,
                Message = "You are not authorized to remove the company branch assignment.",
            };
        }

        return new BranchAuthorizationResult();
    }

    public static bool CanEditInvoice(Invoice invoice) =>
        invoice.ApprovalStatus == InvoiceApprovalStatus.Draft
        || invoice.ApprovalStatus == InvoiceApprovalStatus.Rejected;

    public static bool CanDeleteInvoice(Invoice invoice) => CanEditInvoice(invoice);

    public static bool CanSubmitInvoice(Invoice invoice) =>
        invoice.ApprovalStatus == InvoiceApprovalStatus.Draft
        || invoice.ApprovalStatus == InvoiceApprovalStatus.Rejected;

    public static bool CanApproveInvoice(Invoice invoice) =>
        invoice.ApprovalStatus == InvoiceApprovalStatus.Submitted;

    public static bool CanRejectInvoice(Invoice invoice) =>
        invoice.ApprovalStatus == InvoiceApprovalStatus.Submitted;

    public static bool CanMarkInvoicePaid(Invoice invoice) =>
        invoice.ApprovalStatus == InvoiceApprovalStatus.Approved;
}
