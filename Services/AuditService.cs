using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public interface IAuditService
{
    Task LogAsync(string? userId, string? userName, string action, string entityName, string? recordId = null, string? oldValue = null, string? newValue = null, string result = "Success", string? details = null, string? ipAddress = null);
    Task<List<AuditLog>> GetLogsAsync(string? userId = null, string? entityName = null, string? action = null, DateTime? fromDate = null, DateTime? toDate = null);
}

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;

    public AuditService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(
        string? userId,
        string? userName,
        string action,
        string entityName,
        string? recordId = null,
        string? oldValue = null,
        string? newValue = null,
        string result = "Success",
        string? details = null,
        string? ipAddress = null)
    {
        try
        {
            var log = new AuditLog
            {
                UserId = userId,
                UserName = userName ?? "System/Anonymous",
                Action = action,
                EntityName = entityName,
                RecordId = recordId,
                OldValue = oldValue,
                NewValue = newValue,
                Result = result,
                Details = details,
                IpAddress = ipAddress,
                CreatedDate = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Do not fail application flow if audit write fails, but log to stderr/console
            Console.Error.WriteLine($"[AuditService Error]: {ex.Message}");
        }
    }

    public async Task<List<AuditLog>> GetLogsAsync(
        string? userId = null,
        string? entityName = null,
        string? action = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(a => a.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            query = query.Where(a => a.EntityName.ToLower() == entityName.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(a => a.Action.ToLower() == action.ToLower());
        }

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.CreatedDate >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(a => a.CreatedDate <= endOfDay);
        }

        return await query.OrderByDescending(a => a.CreatedDate).Take(1000).ToListAsync();
    }
}
