namespace ErpApi.Authorization;

public static class PermissionConstants
{
    public const string CompanyRead = "company.read";
    public const string CompanyWrite = "company.write";
    public const string CompanyDelete = "company.delete";
    public const string InvoiceRead = "invoice.read";
    public const string InvoiceWrite = "invoice.write";
    public const string InvoiceDelete = "invoice.delete";
    public const string AuditRead = "audit.read";

    public static readonly string[] All =
    [
        CompanyRead,
        CompanyWrite,
        CompanyDelete,
        InvoiceRead,
        InvoiceWrite,
        InvoiceDelete,
        AuditRead,
    ];
}
