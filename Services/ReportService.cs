using System.Text;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public interface IReportService
{
    Task<List<Customer>> GetCustomerReportAsync(string? userId, string role, string? status, DateTime? fromDate, DateTime? toDate);
    Task<List<Lead>> GetLeadReportAsync(string? userId, string role, string? source, string? status, DateTime? fromDate, DateTime? toDate);
    Task<List<FollowUp>> GetFollowUpReportAsync(string? userId, string role, string? status, DateTime? fromDate, DateTime? toDate);
    Task<List<Opportunity>> GetOpportunityReportAsync(string? userId, string role, string? stage, string? status, DateTime? fromDate, DateTime? toDate);
    Task<PipelineReportDto> GetPipelineReportAsync(string? userId, string role);
    Task<ConversionReportDto> GetConversionReportAsync(string? userId, string role);
    Task<List<UserActivitySummaryDto>> GetUserActivityReportAsync(DateTime? fromDate, DateTime? toDate);
    Task<List<AuditLog>> GetAuditReportAsync(string? userId, string? entityName, string? action, DateTime? fromDate, DateTime? toDate);
    byte[] GenerateCsv<T>(IEnumerable<T> data, Func<T, string[]> rowMapper, string[] headers);
}

public class ConversionReportDto
{
    public int TotalLeads { get; set; }
    public int ConvertedLeads { get; set; }
    public decimal LeadConversionRate { get; set; }
    public int TotalOpportunities { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }
    public decimal WinRate { get; set; }
    public decimal TotalWonRevenue { get; set; }
}

public class UserActivitySummaryDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int LoginCount { get; set; }
    public int CreateCount { get; set; }
    public int UpdateCount { get; set; }
    public int DeleteCount { get; set; }
    public int TotalActions { get; set; }
    public DateTime? LastActive { get; set; }
}

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;
    private readonly IOpportunityService _opportunityService;

    public ReportService(ApplicationDbContext context, IOpportunityService opportunityService)
    {
        _context = context;
        _opportunityService = opportunityService;
    }

    public async Task<List<Customer>> GetCustomerReportAsync(string? userId, string role, string? status, DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.Customers.Include(c => c.Owner).AsNoTracking().AsQueryable();

        if (role == UserRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
        {
            query = query.Where(c => c.CreatedBy == userId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status.ToLower() == status.ToLower());
        }

        if (fromDate.HasValue) query = query.Where(c => c.CreatedDate >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(c => c.CreatedDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));

        return await query.OrderByDescending(c => c.CreatedDate).ToListAsync();
    }

    public async Task<List<Lead>> GetLeadReportAsync(string? userId, string role, string? source, string? status, DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.Leads.Include(l => l.AssignedUser).Include(l => l.ConvertedCustomer).AsNoTracking().AsQueryable();

        if (role == UserRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
        {
            query = query.Where(l => l.AssignedTo == userId);
        }

        if (!string.IsNullOrWhiteSpace(source))
        {
            query = query.Where(l => l.Source.ToLower() == source.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(l => l.Status.ToLower() == status.ToLower());
        }

        if (fromDate.HasValue) query = query.Where(l => l.CreatedDate >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(l => l.CreatedDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));

        return await query.OrderByDescending(l => l.CreatedDate).ToListAsync();
    }

    public async Task<List<FollowUp>> GetFollowUpReportAsync(string? userId, string role, string? status, DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.AssignedUser)
            .AsNoTracking().AsQueryable();

        if (role == UserRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
        {
            query = query.Where(f => f.AssignedTo == userId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(f => f.Status.ToLower() == status.ToLower());
        }

        if (fromDate.HasValue) query = query.Where(f => f.FollowUpDate >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(f => f.FollowUpDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));

        return await query.OrderBy(f => f.FollowUpDate).ToListAsync();
    }

    public async Task<List<Opportunity>> GetOpportunityReportAsync(string? userId, string role, string? stage, string? status, DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .Include(o => o.AssignedUser)
            .AsNoTracking().AsQueryable();

        if (role == UserRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
        {
            query = query.Where(o => o.AssignedTo == userId);
        }

        if (!string.IsNullOrWhiteSpace(stage))
        {
            query = query.Where(o => o.Stage.ToLower() == stage.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o => o.Status.ToLower() == status.ToLower());
        }

        if (fromDate.HasValue) query = query.Where(o => o.CreatedDate >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(o => o.CreatedDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));

        return await query.OrderByDescending(o => o.CreatedDate).ToListAsync();
    }

    public async Task<PipelineReportDto> GetPipelineReportAsync(string? userId, string role)
    {
        return await _opportunityService.GetPipelineSummaryAsync(userId, role);
    }

    public async Task<ConversionReportDto> GetConversionReportAsync(string? userId, string role)
    {
        var leadQuery = _context.Leads.AsNoTracking().AsQueryable();
        var oppQuery = _context.Opportunities.AsNoTracking().AsQueryable();

        if (role == UserRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
        {
            leadQuery = leadQuery.Where(l => l.AssignedTo == userId);
            oppQuery = oppQuery.Where(o => o.AssignedTo == userId);
        }

        var totalLeads = await leadQuery.CountAsync();
        var convertedLeads = await leadQuery.CountAsync(l => l.Status == LeadStatuses.Converted);

        var opps = await oppQuery.ToListAsync();
        var totalOpps = opps.Count;
        var wonOpps = opps.Count(o => o.Status == OpportunityStatuses.Won);
        var lostOpps = opps.Count(o => o.Status == OpportunityStatuses.Lost);
        var wonRevenue = opps.Where(o => o.Status == OpportunityStatuses.Won).Sum(o => o.Amount);

        var leadConvRate = totalLeads > 0 ? Math.Round((decimal)convertedLeads / totalLeads * 100m, 2) : 0m;
        var closedOpps = wonOpps + lostOpps;
        var winRate = closedOpps > 0 ? Math.Round((decimal)wonOpps / closedOpps * 100m, 2) : 0m;

        return new ConversionReportDto
        {
            TotalLeads = totalLeads,
            ConvertedLeads = convertedLeads,
            LeadConversionRate = leadConvRate,
            TotalOpportunities = totalOpps,
            WonOpportunities = wonOpps,
            LostOpportunities = lostOpps,
            WinRate = winRate,
            TotalWonRevenue = wonRevenue
        };
    }

    public async Task<List<UserActivitySummaryDto>> GetUserActivityReportAsync(DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (fromDate.HasValue) query = query.Where(a => a.CreatedDate >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(a => a.CreatedDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));

        var logs = await query.ToListAsync();

        var grouped = logs
            .GroupBy(l => new { l.UserId, UserName = l.UserName ?? "Unknown" })
            .Select(g => new UserActivitySummaryDto
            {
                UserId = g.Key.UserId ?? "N/A",
                UserName = g.Key.UserName,
                LoginCount = g.Count(x => x.Action == "Login"),
                CreateCount = g.Count(x => x.Action == "Create"),
                UpdateCount = g.Count(x => x.Action == "Update"),
                DeleteCount = g.Count(x => x.Action == "Delete"),
                TotalActions = g.Count(),
                LastActive = g.Max(x => (DateTime?)x.CreatedDate)
            })
            .OrderByDescending(u => u.TotalActions)
            .ToList();

        return grouped;
    }

    public async Task<List<AuditLog>> GetAuditReportAsync(string? userId, string? entityName, string? action, DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId)) query = query.Where(a => a.UserId == userId);
        if (!string.IsNullOrWhiteSpace(entityName)) query = query.Where(a => a.EntityName.ToLower() == entityName.ToLower());
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(a => a.Action.ToLower() == action.ToLower());
        if (fromDate.HasValue) query = query.Where(a => a.CreatedDate >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(a => a.CreatedDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));

        return await query.OrderByDescending(a => a.CreatedDate).Take(2000).ToListAsync();
    }

    public byte[] GenerateCsv<T>(IEnumerable<T> data, Func<T, string[]> rowMapper, string[] headers)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));

        foreach (var item in data)
        {
            var row = rowMapper(item);
            sb.AppendLine(string.Join(",", row.Select(EscapeCsv)));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
