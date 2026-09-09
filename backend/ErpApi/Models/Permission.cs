namespace ErpApi.Models;

public class Permission
{
    public int PermissionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }

    public List<RolePermission> RolePermissions { get; set; } = new();
}
