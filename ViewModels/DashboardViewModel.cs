using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

public class DashboardViewModel
{
    public string DateFilter { get; set; } = "ThisMonth";
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    // Role-based KPI Cards (Section 17.11)
    public int TotalCustomers { get; set; }
    public int TotalLeads { get; set; }
    public int OpenLeads { get; set; }
    public int TotalOpportunities { get; set; }
    public int OpenOpportunities { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }
    public decimal TotalPipelineValue { get; set; }
    public decimal WeightedPipelineValue { get; set; }
    public int PendingFollowUps { get; set; }
    public int OverdueFollowUps { get; set; }

    // Admin / Manager extras
    public int TotalUsers { get; set; }
    public int TotalAuditEvents { get; set; }

    // Chart.js data
    public List<string> LeadStatusLabels { get; set; } = new();
    public List<int> LeadStatusCounts { get; set; } = new();

    public List<string> PipelineStageLabels { get; set; } = new();
    public List<decimal> PipelineStageAmounts { get; set; } = new();
    public List<int> PipelineStageCounts { get; set; } = new();

    public List<string> MonthlySalesLabels { get; set; } = new();
    public List<decimal> MonthlySalesValues { get; set; } = new();

    // Recent items lists for quick dashboard visibility
    public List<FollowUp> UpcomingFollowUps { get; set; } = new();
    public List<Lead> RecentLeads { get; set; } = new();
    public List<Opportunity> TopOpportunities { get; set; } = new();
    public List<AuditLog> RecentAuditLogs { get; set; } = new();
}
