namespace ErpApi.Models;

public class AuditLog
{
    public long AuditLogId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public int? UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? Metadata { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? RequestPath { get; set; }
    public string? HttpMethod { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public User? User { get; set; }
}
