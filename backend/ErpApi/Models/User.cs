namespace ErpApi.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public bool IsActive { get; set; } = true;

        public List<UserRole> UserRoles { get; set; } = new();
        public List<UserBranch> UserBranches { get; set; } = new();
        public List<UserCompany> UserCompanies { get; set; } = new();
    }
}
