namespace ErpApi.DTOs.Invoices;

public class InvoiceItemResponse
{
    public Guid InvoiceItemId { get; set; }
    public Guid InvoiceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public decimal? TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public DateTime CreatedDate { get; set; }
}
