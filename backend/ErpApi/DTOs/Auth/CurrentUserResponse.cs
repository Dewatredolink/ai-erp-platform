namespace ErpApi.DTOs.Auth;

public class CurrentUserResponse : UserSummaryResponse
{
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}
