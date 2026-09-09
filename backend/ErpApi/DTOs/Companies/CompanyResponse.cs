namespace ErpApi.DTOs.Companies;

public class CompanyResponse
{
    public int CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PinCode { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Website { get; set; }
    public string? GSTIN { get; set; }
    public string? PAN { get; set; }
    public string? DrugLicenceNumber { get; set; }
    public string? UdogAadhaar { get; set; }
    public string? AadhaarNumber { get; set; }
    public string? MSMENumber { get; set; }
    public string? FSSAINumber { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public bool IsActive { get; set; }
}
