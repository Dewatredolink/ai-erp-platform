using ErpApi.Data;
using ErpApi.DTOs.Auth;
using ErpApi.Authorization;
using ErpApi.Models;
using ErpApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ErpApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        [FromServices] ErpDbContext db,
        [FromServices] AuditService auditService)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Username, email and password are required." });
        }

        if (request.Password.Length < 8)
        {
            return BadRequest(new { message = "Password must be at least 8 characters long." });
        }

        var normalizedUsername = request.Username.Trim();
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var existingUser = await db.Users.FirstOrDefaultAsync(u =>
            u.Username == normalizedUsername || u.Email == normalizedEmail);

        if (existingUser != null)
        {
            return Conflict(new { message = "Unable to register with the provided credentials." });
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
        await EnsureDefaultRoleAssignmentAsync(db, user);
        await auditService.WriteAsync(
            actionType: "auth.register",
            entityName: nameof(User),
            entityId: user.UserId.ToString(),
            newValues: new { user.UserId, user.Username, user.Email, user.IsActive },
            userId: user.UserId,
            username: user.Username);

        return Created("/api/auth/me", new { user.UserId, user.Username, user.Email });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        [FromServices] ErpDbContext db,
        [FromServices] JwtTokenService tokenService,
        [FromServices] AuditService auditService)
    {
        if (string.IsNullOrWhiteSpace(request.UsernameOrEmail) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Username/email and password are required." });
        }

        var normalizedIdentifier = request.UsernameOrEmail.Trim();
        var normalizedEmail = normalizedIdentifier.ToLowerInvariant();

        var user = await db.Users.FirstOrDefaultAsync(u =>
            u.Username == normalizedIdentifier || u.Email == normalizedEmail);

        if (user == null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid username/email or password." });
        }

        await EnsureDefaultRoleAssignmentAsync(db, user);
        var (roles, permissions) = await GetUserAccessAsync(db, user.UserId);
        var (token, expiresAt) = tokenService.GenerateToken(user, roles, permissions);
        await auditService.WriteAsync(
            actionType: "auth.login",
            entityName: nameof(User),
            entityId: user.UserId.ToString(),
            metadata: new { roles, permissions },
            userId: user.UserId,
            username: user.Username);

        return Ok(new AuthResponse
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            Token = token,
            ExpiresAt = expiresAt,
            Roles = roles,
            Permissions = permissions,
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromServices] AuditService auditService, [FromServices] CurrentUserService currentUserService)
    {
        await auditService.WriteAsync(
            actionType: "auth.logout",
            entityName: nameof(User),
            entityId: currentUserService.UserId?.ToString(),
            userId: currentUserService.UserId,
            username: currentUserService.Username);

        return Ok(new { message = "Logged out successfully." });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser([FromServices] ErpDbContext db)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("userId")
            ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await db.Users.FindAsync(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        await EnsureDefaultRoleAssignmentAsync(db, user);
        var (roles, permissions) = await GetUserAccessAsync(db, user.UserId);

        return Ok(new { user.UserId, user.Username, user.Email, roles, permissions });
    }

    private static async Task EnsureDefaultRoleAssignmentAsync(ErpDbContext db, User user)
    {
        var hasRole = await db.UserRoles.AnyAsync(ur => ur.UserId == user.UserId);
        if (hasRole)
        {
            return;
        }

        var administratorRoleId = await db.Roles
            .Where(role => role.Name == RoleConstants.Administrator)
            .Select(role => role.RoleId)
            .SingleAsync();

        db.UserRoles.Add(new UserRole
        {
            UserId = user.UserId,
            RoleId = administratorRoleId,
            CreatedDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
        });

        await db.SaveChangesAsync();
    }

    private static async Task<(List<string> Roles, List<string> Permissions)> GetUserAccessAsync(ErpDbContext db, int userId)
    {
        var roles = await db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role!.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync();

        var permissions = await db.UserRoles
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => ur.Role!.RolePermissions.Select(rp => rp.Permission!.Name))
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync();

        return (roles, permissions);
    }
}
