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
            var principal = _httpContextAccessor.HttpContext?.User;
            var userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal?.FindFirstValue("userId")
                ?? principal?.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

            return int.TryParse(userId, out var parsedUserId) ? parsedUserId : null;
        }
    }

    public string Username =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name)
        ?? _httpContextAccessor.HttpContext?.User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.UniqueName)
        ?? "Anonymous";
}
