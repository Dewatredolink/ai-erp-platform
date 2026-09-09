namespace ErpApi.Models;

public class Branch
{
    public int BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public bool IsActive { get; set; } = true;
}
