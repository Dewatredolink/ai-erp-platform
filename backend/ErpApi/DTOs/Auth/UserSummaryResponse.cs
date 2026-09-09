namespace ErpApi.DTOs.Auth;

public class UserSummaryResponse
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
