using System.Security.Claims;

namespace ErpApi.Services;

public class CurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? UserId
    {
        get
        {
            return GetUserId(_httpContextAccessor.HttpContext?.User);
        }
    }

    public string Username =>
        GetUsername(_httpContextAccessor.HttpContext?.User);

    public static int? GetUserId(ClaimsPrincipal? principal)
    {
        var userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal?.FindFirstValue("userId")
            ?? principal?.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

        return int.TryParse(userId, out var parsedUserId) ? parsedUserId : null;
    }

    public static string GetUsername(ClaimsPrincipal? principal) =>
        principal?.FindFirstValue(ClaimTypes.Name)
        ?? principal?.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.UniqueName)
        ?? "Anonymous";
}
