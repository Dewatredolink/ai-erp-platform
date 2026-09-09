using System.Security.Claims;
using ErpApi.Data;
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

        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("userId")
            ?? context.User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (!int.TryParse(userIdValue, out var userId))
        {
            return;
        }

        var hasPermission = await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => ur.Role!.RolePermissions.Select(rp => rp.Permission!.Name))
            .AnyAsync(permission => permission == requirement.Permission);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}
