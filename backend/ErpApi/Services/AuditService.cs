using System.Text.Json;
using ErpApi.Data;
using ErpApi.Middleware;
using ErpApi.Models;

namespace ErpApi.Services;

public class AuditService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ErpDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CurrentUserService _currentUserService;

    public AuditService(
        ErpDbContext db,
        IHttpContextAccessor httpContextAccessor,
        CurrentUserService currentUserService)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        _currentUserService = currentUserService;
    }

    public Task WriteAsync(
        string actionType,
        string entityName,
        string? entityId = null,
        object? oldValues = null,
        object? newValues = null,
        object? metadata = null,
        int? userId = null,
        string? username = null,
        CancellationToken cancellationToken = default)
    {
        var context = _httpContextAccessor.HttpContext;
        var auditLog = new AuditLog
        {
            ActionType = actionType,
            EntityName = entityName,
            EntityId = entityId,
            UserId = userId ?? _currentUserService.UserId,
            Username = username ?? _currentUserService.Username,
            CreatedDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            OldValues = Serialize(oldValues),
            NewValues = Serialize(newValues),
            Metadata = Serialize(metadata),
            CorrelationId = context?.Items[CorrelationIdMiddleware.ItemKey]?.ToString() ?? context?.TraceIdentifier ?? string.Empty,
            RequestPath = context?.Request.Path.Value,
            HttpMethod = context?.Request.Method,
            IpAddress = context?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = context?.Request.Headers.UserAgent.ToString(),
        };

        _db.AuditLogs.Add(auditLog);
        return _db.SaveChangesAsync(cancellationToken);
    }

    private static string? Serialize(object? value)
    {
        return value == null ? null : JsonSerializer.Serialize(value, SerializerOptions);
    }
}
