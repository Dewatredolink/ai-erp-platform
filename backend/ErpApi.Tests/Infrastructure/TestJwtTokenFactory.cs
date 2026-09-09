using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ErpApi.Services;
using Microsoft.IdentityModel.Tokens;

namespace ErpApi.Tests.Infrastructure;

internal static class TestJwtTokenFactory
{
    internal static string CreateToken(JwtSettings settings, int userId, string username, params string[] permissions)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("userId", userId.ToString()),
            new(ClaimTypes.Name, username),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(JwtRegisteredClaimNames.Email, $"{username}@example.com"),
        };

        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
