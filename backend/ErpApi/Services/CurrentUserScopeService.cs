using ErpApi.Authorization;
using ErpApi.Data;
using ErpApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ErpApi.Services;

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
}
