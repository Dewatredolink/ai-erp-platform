using ErpApi.DTOs.Companies;
using ErpApi.Models;

namespace ErpApi.DTOs.Invoices;

public class InvoiceResponse
{
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public CompanyResponse? Company { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public InvoiceStatus Status { get; set; }
    public InvoiceApprovalStatus ApprovalStatus { get; set; }
    public int? SubmittedByUserId { get; set; }
    public string? SubmittedBy { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int? ApprovedByUserId { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int? RejectedByUserId { get; set; }
    public string? RejectedBy { get; set; }
    public DateTime? RejectedAt { get; set; }
    public int? PaidByUserId { get; set; }
    public string? PaidBy { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? ApprovalRemarks { get; set; }
    public string? Notes { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime UpdatedDate { get; set; }
    public List<InvoiceItemResponse> Items { get; set; } = new();
}
