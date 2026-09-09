using ErpApi.Authorization;
using ErpApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ErpApi.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Policy = PermissionConstants.AuditRead)]
public class AuditLogsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAuditLogs(
        [FromServices] ErpDbContext db,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] int? userId = null,
        [FromQuery] string? username = null,
        [FromQuery] string? entityName = null,
        [FromQuery] string? entityId = null,
        [FromQuery] string? actionType = null,
        [FromQuery] string? correlationId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 50;

        var query = BuildFilteredQuery(
            db,
            userId,
            username,
            entityName,
            entityId,
            actionType,
            correlationId,
            fromDate,
            toDate);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(log => log.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            items,
            totalCount,
            page,
            pageSize,
        });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetAuditLogById([FromServices] ErpDbContext db, long id)
    {
        var item = await db.AuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(log => log.AuditLogId == id);

        return item == null ? NotFound() : Ok(item);
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportAuditLogs(
        [FromServices] ErpDbContext db,
        [FromQuery] string format = "json",
        [FromQuery] int? userId = null,
        [FromQuery] string? username = null,
        [FromQuery] string? entityName = null,
        [FromQuery] string? entityId = null,
        [FromQuery] string? actionType = null,
        [FromQuery] string? correlationId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        const int maxExportRows = 5000;
        var normalizedFormat = format.Trim().ToLowerInvariant();
        if (normalizedFormat != "json" && normalizedFormat != "csv")
        {
            return BadRequest(new { message = "Unsupported export format. Allowed values: json, csv." });
        }

        var items = await BuildFilteredQuery(
                db,
                userId,
                username,
                entityName,
                entityId,
                actionType,
                correlationId,
                fromDate,
                toDate)
            .OrderByDescending(log => log.CreatedDate)
            .Take(maxExportRows)
            .ToListAsync();

        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        if (normalizedFormat == "json")
        {
            return Ok(new
            {
                exportedAt = DateTime.UtcNow,
                count = items.Count,
                items,
            });
        }

        var csv = BuildCsv(items);
        return File(
            Encoding.UTF8.GetBytes(csv),
            "text/csv",
            $"audit-logs-{timestamp}.csv");
    }

    private static IQueryable<Models.AuditLog> BuildFilteredQuery(
        ErpDbContext db,
        int? userId,
        string? username,
        string? entityName,
        string? entityId,
        string? actionType,
        string? correlationId,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var query = db.AuditLogs.AsNoTracking().AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(log => log.UserId == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(username))
        {
            var normalizedUsername = username.Trim();
            query = query.Where(log => log.Username.Contains(normalizedUsername));
        }

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            query = query.Where(log => log.EntityName == entityName.Trim());
        }

        if (!string.IsNullOrWhiteSpace(entityId))
        {
            query = query.Where(log => log.EntityId == entityId.Trim());
        }

        if (!string.IsNullOrWhiteSpace(actionType))
        {
            query = query.Where(log => log.ActionType == actionType.Trim());
        }

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            var normalizedCorrelationId = correlationId.Trim();
            query = query.Where(log => log.CorrelationId.Contains(normalizedCorrelationId));
        }

        if (fromDate.HasValue)
        {
            query = query.Where(log => log.CreatedDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(log => log.CreatedDate <= toDate.Value);
        }

        return query;
    }

    private static string BuildCsv(IEnumerable<Models.AuditLog> items)
    {
        var builder = new StringBuilder();
        builder.AppendLine("auditLogId,createdDate,username,userId,entityName,entityId,actionType,correlationId,requestPath,httpMethod,ipAddress,userAgent,oldValues,newValues,metadata");
        foreach (var item in items)
        {
            builder
                .Append(item.AuditLogId).Append(',')
                .Append(ToCsvValue(item.CreatedDate.ToString("O"))).Append(',')
                .Append(ToCsvValue(item.Username)).Append(',')
                .Append(ToCsvValue(item.UserId?.ToString())).Append(',')
                .Append(ToCsvValue(item.EntityName)).Append(',')
                .Append(ToCsvValue(item.EntityId)).Append(',')
                .Append(ToCsvValue(item.ActionType)).Append(',')
                .Append(ToCsvValue(item.CorrelationId)).Append(',')
                .Append(ToCsvValue(item.RequestPath)).Append(',')
                .Append(ToCsvValue(item.HttpMethod)).Append(',')
                .Append(ToCsvValue(item.IpAddress)).Append(',')
                .Append(ToCsvValue(item.UserAgent)).Append(',')
                .Append(ToCsvValue(item.OldValues)).Append(',')
                .Append(ToCsvValue(item.NewValues)).Append(',')
                .Append(ToCsvValue(item.Metadata))
                .AppendLine();
        }
        return builder.ToString();
    }

    private static string ToCsvValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
