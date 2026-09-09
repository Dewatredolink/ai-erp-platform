namespace ErpApi.Models;

public class UserCompany
{
    public int UserId { get; set; }
    public int CompanyId { get; set; }
    public DateTime CreatedDate { get; set; }

    public User? User { get; set; }
    public Company? Company { get; set; }
}
