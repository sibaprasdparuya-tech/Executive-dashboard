using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public interface IOpportunityService
{
    Task<List<Opportunity>> GetAllAsync(string? userId, string role, string? searchTerm = null, string? stage = null, string? status = null);
    Task<Opportunity?> GetByIdAsync(int id, string? userId, string role);
    Task<(bool Success, string? ErrorMessage, Opportunity? Opportunity)> CreateAsync(Opportunity opportunity, string? currentUserId, string? currentUserName, string? ipAddress);
    Task<(bool Success, string? ErrorMessage)> UpdateAsync(Opportunity opportunity, string? currentUserId, string? currentUserName, string? ipAddress);
    Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id, string? currentUserId, string? currentUserName, string? ipAddress);
    Task<PipelineReportDto> GetPipelineSummaryAsync(string? userId, string role);
}

public class OpportunityService : IOpportunityService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public OpportunityService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<List<Opportunity>> GetAllAsync(string? userId, string role, string? searchTerm = null, string? stage = null, string? status = null)
    {
        var query = _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .Include(o => o.AssignedUser)
            .AsNoTracking()
            .AsQueryable();

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

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(o =>
                o.OpportunityName.ToLower().Contains(term) ||
                (o.Customer != null && o.Customer.CustomerName.ToLower().Contains(term)) ||
                (o.Lead != null && o.Lead.LeadName.ToLower().Contains(term)));
        }

        return await query.OrderByDescending(o => o.CreatedDate).ToListAsync();
    }

    public async Task<Opportunity?> GetByIdAsync(int id, string? userId, string role)
    {
        var query = _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .Include(o => o.AssignedUser)
            .Include(o => o.FollowUps)
            .Include(o => o.Activities)
            .AsQueryable();

        if (role == UserRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
        {
            query = query.Where(o => o.AssignedTo == userId);
        }

        return await query.FirstOrDefaultAsync(o => o.OpportunityId == id);
    }

    public async Task<(bool Success, string? ErrorMessage, Opportunity? Opportunity)> CreateAsync(
        Opportunity opportunity, string? currentUserId, string? currentUserName, string? ipAddress)
    {
        // Business Validation 1: Amount > 0
        if (opportunity.Amount <= 0)
        {
            return (false, "Opportunity Amount must be greater than 0.", null);
        }

        // Business Validation 2: Probability between 0 and 100
        if (opportunity.Probability < 0 || opportunity.Probability > 100)
        {
            return (false, "Probability must be between 0 and 100.", null);
        }

        // Business Validation 3: Expected Close Date cannot be in the past for active opportunity
        if (opportunity.Status == OpportunityStatuses.Open && opportunity.ExpectedCloseDate.Date < DateTime.Today)
        {
            return (false, "Expected Close Date cannot be in the past.", null);
        }

        // Synchronize Status with Stage
        if (opportunity.Stage == OpportunityStages.Won)
        {
            opportunity.Status = OpportunityStatuses.Won;
        }
        else if (opportunity.Stage == OpportunityStages.Lost)
        {
            opportunity.Status = OpportunityStatuses.Lost;
        }
        else
        {
            opportunity.Status = OpportunityStatuses.Open;
        }

        if (string.IsNullOrEmpty(opportunity.AssignedTo))
        {
            opportunity.AssignedTo = currentUserId;
        }

        opportunity.CreatedDate = DateTime.UtcNow;

        _context.Opportunities.Add(opportunity);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Create",
            "Opportunity",
            opportunity.OpportunityId.ToString(),
            null,
            $"{opportunity.OpportunityName} - ${opportunity.Amount:N2} ({opportunity.Stage})",
            "Success",
            $"Created Opportunity: {opportunity.OpportunityName}",
            ipAddress);

        return (true, null, opportunity);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateAsync(
        Opportunity opportunity, string? currentUserId, string? currentUserName, string? ipAddress)
    {
        var existing = await _context.Opportunities.FindAsync(opportunity.OpportunityId);
        if (existing == null)
        {
            return (false, "Opportunity not found.");
        }

        // Business Validation 1: Amount > 0
        if (opportunity.Amount <= 0)
        {
            return (false, "Opportunity Amount must be greater than 0.");
        }

        // Business Validation 2: Probability between 0 and 100
        if (opportunity.Probability < 0 || opportunity.Probability > 100)
        {
            return (false, "Probability must be between 0 and 100.");
        }

        // Synchronize Status with Stage
        if (opportunity.Stage == OpportunityStages.Won)
        {
            opportunity.Status = OpportunityStatuses.Won;
        }
        else if (opportunity.Stage == OpportunityStages.Lost)
        {
            opportunity.Status = OpportunityStatuses.Lost;
        }
        else
        {
            opportunity.Status = OpportunityStatuses.Open;
        }

        // Business Validation 3: Expected Close Date cannot be in the past for active opportunity
        if (opportunity.Status == OpportunityStatuses.Open && opportunity.ExpectedCloseDate.Date < DateTime.Today)
        {
            return (false, "Expected Close Date cannot be in the past.");
        }

        var oldSummary = $"{existing.OpportunityName}, Stage: {existing.Stage}, Amount: ${existing.Amount:N2}, Prob: {existing.Probability}%";

        existing.OpportunityName = opportunity.OpportunityName;
        existing.CustomerId = opportunity.CustomerId;
        existing.LeadId = opportunity.LeadId;
        existing.Amount = opportunity.Amount;
        existing.Stage = opportunity.Stage;
        existing.Probability = opportunity.Probability;
        existing.ExpectedCloseDate = opportunity.ExpectedCloseDate;
        existing.Status = opportunity.Status;
        existing.Notes = opportunity.Notes;

        if (!string.IsNullOrEmpty(opportunity.AssignedTo))
        {
            existing.AssignedTo = opportunity.AssignedTo;
        }

        await _context.SaveChangesAsync();

        var newSummary = $"{existing.OpportunityName}, Stage: {existing.Stage}, Amount: ${existing.Amount:N2}, Prob: {existing.Probability}%";

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Update",
            "Opportunity",
            existing.OpportunityId.ToString(),
            oldSummary,
            newSummary,
            "Success",
            $"Updated Opportunity: {existing.OpportunityName}",
            ipAddress);

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteAsync(
        int id, string? currentUserId, string? currentUserName, string? ipAddress)
    {
        var opp = await _context.Opportunities.FindAsync(id);
        if (opp == null)
        {
            return (false, "Opportunity not found.");
        }

        var summary = $"{opp.OpportunityName} (${opp.Amount:N2})";
        _context.Opportunities.Remove(opp);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Delete",
            "Opportunity",
            id.ToString(),
            summary,
            null,
            "Success",
            $"Deleted Opportunity: {summary}",
            ipAddress);

        return (true, null);
    }

    public async Task<PipelineReportDto> GetPipelineSummaryAsync(string? userId, string role)
    {
        var query = _context.Opportunities
            .Include(o => o.AssignedUser)
            .AsNoTracking()
            .AsQueryable();

        if (role == UserRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
        {
            query = query.Where(o => o.AssignedTo == userId);
        }

        var opps = await query.ToListAsync();

        var openOpps = opps.Where(o => o.Status == OpportunityStatuses.Open).ToList();

        var totalPipeline = openOpps.Sum(o => o.Amount);
        var totalWeighted = openOpps.Sum(o => o.WeightedPipeline);

        var byStage = opps
            .GroupBy(o => o.Stage)
            .Select(g => new StageSummaryDto
            {
                Stage = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(x => x.Amount),
                WeightedAmount = g.Sum(x => x.WeightedPipeline)
            })
            .ToList();

        var byOwner = opps
            .GroupBy(o => o.AssignedUser?.FullName ?? o.AssignedUser?.UserName ?? "Unassigned")
            .Select(g => new OwnerSummaryDto
            {
                OwnerName = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(x => x.Amount),
                WeightedAmount = g.Sum(x => x.WeightedPipeline)
            })
            .ToList();

        return new PipelineReportDto
        {
            TotalPipelineValue = totalPipeline,
            TotalWeightedPipelineValue = totalWeighted,
            TotalOpportunitiesCount = opps.Count,
            ByStage = byStage,
            ByOwner = byOwner
        };
    }
}
