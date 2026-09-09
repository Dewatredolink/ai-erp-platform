namespace ErpApi.Models;

public class RolePermission
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }
    public DateTime CreatedDate { get; set; }

    public Role? Role { get; set; }
    public Permission? Permission { get; set; }
}
