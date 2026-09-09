namespace ErpApi.Authorization;

public static class PermissionConstants
{
    public const string CompanyRead = "company.read";
    public const string CompanyWrite = "company.write";
    public const string CompanyDelete = "company.delete";
    public const string InvoiceRead = "invoice.read";
    public const string InvoiceWrite = "invoice.write";
    public const string InvoiceDelete = "invoice.delete";
    public const string InvoiceSubmit = "invoice.submit";
    public const string InvoiceApprove = "invoice.approve";
    public const string InvoiceReject = "invoice.reject";
    public const string InvoicePay = "invoice.pay";
    public const string AuditRead = "audit.read";

    public static readonly string[] All =
    [
        CompanyRead,
        CompanyWrite,
        CompanyDelete,
        InvoiceRead,
        InvoiceWrite,
        InvoiceDelete,
        InvoiceSubmit,
        InvoiceApprove,
        InvoiceReject,
        InvoicePay,
        AuditRead,
    ];
}
