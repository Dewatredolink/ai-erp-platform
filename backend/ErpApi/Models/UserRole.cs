namespace ErpApi.Models;

public class UserRole
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public DateTime CreatedDate { get; set; }

    public User? User { get; set; }
    public Role? Role { get; set; }
}
