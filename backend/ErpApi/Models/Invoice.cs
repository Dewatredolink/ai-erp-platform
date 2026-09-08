using System.Collections.Generic;

namespace ErpApi.Models
{
    public enum InvoiceStatus
    {
        Draft,
        Sent,
        Paid,
        Overdue
    }

    public class Invoice
    {
        public Guid InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public Company? Company { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
        public string? Notes { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTime UpdatedDate { get; set; }

        public List<InvoiceItem> Items { get; set; } = new();
    }
}
