using ErpApi.Models;

namespace ErpApi.DTOs.Invoices;

public class InvoiceRequest
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public string? Notes { get; set; }
    public string? CreatedBy { get; set; }
    public List<InvoiceItemRequest> Items { get; set; } = new();
}
