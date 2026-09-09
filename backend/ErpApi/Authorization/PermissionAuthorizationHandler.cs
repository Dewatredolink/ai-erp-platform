using ErpApi.Data;
using ErpApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace ErpApi.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ErpDbContext _db;

    public PermissionAuthorizationHandler(ErpDbContext db)
    {
        _db = db;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return;
        }

        if (context.User.IsInRole(RoleConstants.Administrator) ||
            context.User.HasClaim("permission", requirement.Permission))
        {
            context.Succeed(requirement);
            return;
        }

        var userId = CurrentUserService.GetUserId(context.User);
        if (!userId.HasValue)
        {
            return;
        }

        var hasPermission = await _db.UserRoles
            .Where(ur => ur.UserId == userId.Value)
            .SelectMany(ur => ur.Role!.RolePermissions.Select(rp => rp.Permission!.Name))
            .AnyAsync(permission => permission == requirement.Permission);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}
