using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardDataAsync(string? userId, string role, string dateFilter, DateTime? customFrom = null, DateTime? customTo = null);
}

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardViewModel> GetDashboardDataAsync(
        string? userId, string role, string dateFilter, DateTime? customFrom = null, DateTime? customTo = null)
    {
        var vm = new DashboardViewModel
        {
            DateFilter = dateFilter,
            FromDate = customFrom,
            ToDate = customTo
        };

        // Determine date range
        DateTime? startDate = null;
        DateTime? endDate = null;
        var now = DateTime.UtcNow;

        switch (dateFilter)
        {
            case "Today":
                startDate = DateTime.Today;
                endDate = DateTime.Today.AddDays(1).AddTicks(-1);
                break;
            case "ThisWeek":
                var diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
                startDate = DateTime.Today.AddDays(-1 * diff);
                endDate = startDate.Value.AddDays(7).AddTicks(-1);
                break;
            case "ThisMonth":
                startDate = new DateTime(now.Year, now.Month, 1);
                endDate = startDate.Value.AddMonths(1).AddTicks(-1);
                break;
            case "Custom":
                startDate = customFrom?.Date;
                endDate = customTo?.Date.AddDays(1).AddTicks(-1);
                break;
            default: // All time or default to month
                break;
        }

        // Base queries
        var custQuery = _context.Customers.AsNoTracking().AsQueryable();
        var leadQuery = _context.Leads.AsNoTracking().AsQueryable();
        var oppQuery = _context.Opportunities.AsNoTracking().AsQueryable();
        var followQuery = _context.FollowUps.AsNoTracking().AsQueryable();

        // Scope by role
        if (role == UserRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
        {
            custQuery = custQuery.Where(c => c.CreatedBy == userId);
            leadQuery = leadQuery.Where(l => l.AssignedTo == userId);
            oppQuery = oppQuery.Where(o => o.AssignedTo == userId);
            followQuery = followQuery.Where(f => f.AssignedTo == userId);
        }

        // Filter by date where applicable
        if (startDate.HasValue)
        {
            custQuery = custQuery.Where(c => c.CreatedDate >= startDate.Value);
            leadQuery = leadQuery.Where(l => l.CreatedDate >= startDate.Value);
            oppQuery = oppQuery.Where(o => o.CreatedDate >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            custQuery = custQuery.Where(c => c.CreatedDate <= endDate.Value);
            leadQuery = leadQuery.Where(l => l.CreatedDate <= endDate.Value);
            oppQuery = oppQuery.Where(o => o.CreatedDate <= endDate.Value);
        }

        // Calculate KPI values
        vm.TotalCustomers = await custQuery.CountAsync();
        vm.TotalLeads = await leadQuery.CountAsync();
        vm.OpenLeads = await leadQuery.CountAsync(l => l.Status != LeadStatuses.Converted && l.Status != LeadStatuses.Lost);

        var oppsList = await oppQuery.ToListAsync();
        vm.TotalOpportunities = oppsList.Count;
        vm.OpenOpportunities = oppsList.Count(o => o.Status == OpportunityStatuses.Open);
        vm.WonOpportunities = oppsList.Count(o => o.Status == OpportunityStatuses.Won);
        vm.LostOpportunities = oppsList.Count(o => o.Status == OpportunityStatuses.Lost);

        var openOpps = oppsList.Where(o => o.Status == OpportunityStatuses.Open).ToList();
        vm.TotalPipelineValue = openOpps.Sum(o => o.Amount);
        vm.WeightedPipelineValue = openOpps.Sum(o => o.WeightedPipeline);

        var followsList = await followQuery.ToListAsync();
        vm.PendingFollowUps = followsList.Count(f => f.Status == FollowUpStatuses.Planned);
        vm.OverdueFollowUps = followsList.Count(f => f.Status == FollowUpStatuses.Planned && f.FollowUpDate.Date < DateTime.Today);

        if (role == UserRoles.Admin)
        {
            vm.TotalUsers = await _context.Users.CountAsync();
            vm.TotalAuditEvents = await _context.AuditLogs.CountAsync();
            vm.RecentAuditLogs = await _context.AuditLogs
                .OrderByDescending(a => a.CreatedDate)
                .Take(5)
                .ToListAsync();
        }

        // Chart 1: Lead Status (New, Contacted, Qualified, Lost, Converted)
        var allLeads = await leadQuery.ToListAsync();
        var leadStatuses = new[] { LeadStatuses.New, LeadStatuses.Contacted, LeadStatuses.Qualified, LeadStatuses.Lost, LeadStatuses.Converted };
        foreach (var st in leadStatuses)
        {
            vm.LeadStatusLabels.Add(st);
            vm.LeadStatusCounts.Add(allLeads.Count(l => l.Status == st));
        }

        // Chart 2: Opportunity Pipeline (Qualification, Proposal, Negotiation, Won, Lost)
        var pipelineStages = new[] { OpportunityStages.Qualification, OpportunityStages.Proposal, OpportunityStages.Negotiation, OpportunityStages.Won, OpportunityStages.Lost };
        foreach (var stg in pipelineStages)
        {
            vm.PipelineStageLabels.Add(stg);
            var stageOpps = oppsList.Where(o => o.Stage == stg).ToList();
            vm.PipelineStageAmounts.Add(stageOpps.Sum(o => o.Amount));
            vm.PipelineStageCounts.Add(stageOpps.Count);
        }

        // Chart 3: Monthly Sales / Won Outcome (last 6 months)
        for (int i = 5; i >= 0; i--)
        {
            var mDate = now.AddMonths(-i);
            var monthName = mDate.ToString("MMM yyyy");
            vm.MonthlySalesLabels.Add(monthName);

            var monthWonTotal = oppsList
                .Where(o => o.Status == OpportunityStatuses.Won && o.CreatedDate.Month == mDate.Month && o.CreatedDate.Year == mDate.Year)
                .Sum(o => o.Amount);

            vm.MonthlySalesValues.Add(monthWonTotal);
        }

        // Recent items
        vm.UpcomingFollowUps = await followQuery
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Where(f => f.Status == FollowUpStatuses.Planned)
            .OrderBy(f => f.FollowUpDate)
            .Take(5)
            .ToListAsync();

        vm.RecentLeads = await leadQuery
            .OrderByDescending(l => l.CreatedDate)
            .Take(5)
            .ToListAsync();

        vm.TopOpportunities = await oppQuery
            .Include(o => o.Customer)
            .OrderByDescending(o => o.Amount)
            .Take(5)
            .ToListAsync();

        return vm;
    }
}
