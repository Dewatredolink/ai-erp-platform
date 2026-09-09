using ErpApi.Authorization;
using ErpApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        [FromQuery] string? entityName = null,
        [FromQuery] string? actionType = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 50;

        var query = db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            query = query.Where(log => log.EntityName == entityName.Trim());
        }

        if (!string.IsNullOrWhiteSpace(actionType))
        {
            query = query.Where(log => log.ActionType == actionType.Trim());
        }

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
}
