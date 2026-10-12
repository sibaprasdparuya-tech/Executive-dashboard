using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public interface IFollowUpService
{
    Task<List<FollowUp>> GetAllAsync(string? userId, string role, string? searchTerm = null, string? status = null, DateTime? date = null, bool? overdueOnly = null);
    Task<FollowUp?> GetByIdAsync(int id, string? userId, string role);
    Task<(bool Success, string? ErrorMessage, FollowUp? FollowUp)> CreateAsync(FollowUp followUp, string? currentUserId, string? currentUserName, string? ipAddress);
    Task<(bool Success, string? ErrorMessage)> UpdateAsync(FollowUp followUp, string? currentUserId, string? currentUserName, string? ipAddress);
    Task<(bool Success, string? ErrorMessage)> CompleteFollowUpAsync(int id, string? remarks, string? currentUserId, string? currentUserName, string? ipAddress);
    Task<(bool Success, string? ErrorMessage)> RescheduleFollowUpAsync(int id, DateTime newDate, string? remarks, string? currentUserId, string? currentUserName, string? ipAddress);
    Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id, string? currentUserId, string? currentUserName, string? ipAddress);
    Task<int> GetPendingCountAsync(string? userId, string role);
    Task<int> GetOverdueCountAsync(string? userId, string role);
}

public class FollowUpService : IFollowUpService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public FollowUpService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<List<FollowUp>> GetAllAsync(
        string? userId, string role, string? searchTerm = null, string? status = null, DateTime? date = null, bool? overdueOnly = null)
    {
        var query = _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.Opportunity)
            .Include(f => f.AssignedUser)
            .AsNoTracking()
            .AsQueryable();

        if (role == UserRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
        {
            query = query.Where(f => f.AssignedTo == userId);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(f => f.Status.ToLower() == status.ToLower());
        }

        if (date.HasValue)
        {
            query = query.Where(f => f.FollowUpDate.Date == date.Value.Date);
        }

        if (overdueOnly == true)
        {
            query = query.Where(f => f.Status == FollowUpStatuses.Planned && f.FollowUpDate.Date < DateTime.Today);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(f =>
                f.Subject.ToLower().Contains(term) ||
                (f.Customer != null && f.Customer.CustomerName.ToLower().Contains(term)) ||
                (f.Lead != null && f.Lead.LeadName.ToLower().Contains(term)) ||
                (f.Remarks != null && f.Remarks.ToLower().Contains(term)));
        }

        return await query.OrderBy(f => f.FollowUpDate).ToListAsync();
    }

    public async Task<FollowUp?> GetByIdAsync(int id, string? userId, string role)
    {
        var query = _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.Opportunity)
            .Include(f => f.AssignedUser)
            .AsQueryable();

        if (role == UserRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
        {
            query = query.Where(f => f.AssignedTo == userId);
        }

        return await query.FirstOrDefaultAsync(f => f.FollowUpId == id);
    }

    public async Task<(bool Success, string? ErrorMessage, FollowUp? FollowUp)> CreateAsync(
        FollowUp followUp, string? currentUserId, string? currentUserName, string? ipAddress)
    {
        // Business Validation: Follow-up date cannot be earlier than today for a new/planned follow-up
        if (followUp.Status == FollowUpStatuses.Planned && followUp.FollowUpDate.Date < DateTime.Today)
        {
            return (false, "Follow-up date cannot be earlier than today.", null);
        }

        if (string.IsNullOrEmpty(followUp.AssignedTo))
        {
            followUp.AssignedTo = currentUserId;
        }

        followUp.CreatedDate = DateTime.UtcNow;

        _context.FollowUps.Add(followUp);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Create",
            "FollowUp",
            followUp.FollowUpId.ToString(),
            null,
            $"{followUp.Subject} on {followUp.FollowUpDate:yyyy-MM-dd}",
            "Success",
            $"Scheduled Follow-up: {followUp.Subject}",
            ipAddress);

        return (true, null, followUp);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateAsync(
        FollowUp followUp, string? currentUserId, string? currentUserName, string? ipAddress)
    {
        var existing = await _context.FollowUps.FindAsync(followUp.FollowUpId);
        if (existing == null)
        {
            return (false, "Follow-up not found.");
        }

        // Validate date if planned
        if (followUp.Status == FollowUpStatuses.Planned && followUp.FollowUpDate.Date < DateTime.Today)
        {
            return (false, "Follow-up date cannot be earlier than today.");
        }

        var oldSummary = $"{existing.Subject}, Date: {existing.FollowUpDate:yyyy-MM-dd}, Status: {existing.Status}";

        existing.Subject = followUp.Subject;
        existing.FollowUpType = followUp.FollowUpType;
        existing.FollowUpDate = followUp.FollowUpDate;
        existing.Status = followUp.Status;
        existing.Remarks = followUp.Remarks;
        existing.Notes = followUp.Notes;
        existing.CustomerId = followUp.CustomerId;
        existing.LeadId = followUp.LeadId;
        existing.OpportunityId = followUp.OpportunityId;

        if (!string.IsNullOrEmpty(followUp.AssignedTo))
        {
            existing.AssignedTo = followUp.AssignedTo;
        }

        await _context.SaveChangesAsync();

        var newSummary = $"{existing.Subject}, Date: {existing.FollowUpDate:yyyy-MM-dd}, Status: {existing.Status}";

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Update",
            "FollowUp",
            existing.FollowUpId.ToString(),
            oldSummary,
            newSummary,
            "Success",
            $"Updated Follow-up: {existing.Subject}",
            ipAddress);

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> CompleteFollowUpAsync(
        int id, string? remarks, string? currentUserId, string? currentUserName, string? ipAddress)
    {
        var existing = await _context.FollowUps.FindAsync(id);
        if (existing == null)
        {
            return (false, "Follow-up not found.");
        }

        var oldStatus = existing.Status;
        existing.Status = FollowUpStatuses.Completed;
        if (!string.IsNullOrWhiteSpace(remarks))
        {
            existing.Remarks = (existing.Remarks != null ? existing.Remarks + " | " : "") + remarks;
        }

        // Also add an Activity record
        var activity = new Activity
        {
            ActivityType = existing.FollowUpType,
            Subject = $"Completed Follow-up: {existing.Subject}",
            Description = remarks ?? existing.Remarks,
            ActivityDate = DateTime.UtcNow,
            CustomerId = existing.CustomerId,
            LeadId = existing.LeadId,
            OpportunityId = existing.OpportunityId,
            AssignedTo = currentUserId,
            Status = "Completed",
            CreatedDate = DateTime.UtcNow
        };
        _context.Activities.Add(activity);

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Complete",
            "FollowUp",
            existing.FollowUpId.ToString(),
            oldStatus,
            FollowUpStatuses.Completed,
            "Success",
            $"Completed Follow-up #{id}: {existing.Subject}",
            ipAddress);

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> RescheduleFollowUpAsync(
        int id, DateTime newDate, string? remarks, string? currentUserId, string? currentUserName, string? ipAddress)
    {
        if (newDate.Date < DateTime.Today)
        {
            return (false, "Follow-up date cannot be earlier than today.");
        }

        var existing = await _context.FollowUps.FindAsync(id);
        if (existing == null)
        {
            return (false, "Follow-up not found.");
        }

        var oldDate = existing.FollowUpDate;
        existing.FollowUpDate = newDate;
        existing.Status = FollowUpStatuses.Planned;
        if (!string.IsNullOrWhiteSpace(remarks))
        {
            existing.Remarks = (existing.Remarks != null ? existing.Remarks + " | " : "") + $"Rescheduled: {remarks}";
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Reschedule",
            "FollowUp",
            existing.FollowUpId.ToString(),
            oldDate.ToString("yyyy-MM-dd"),
            newDate.ToString("yyyy-MM-dd"),
            "Success",
            $"Rescheduled Follow-up #{id} to {newDate:yyyy-MM-dd}",
            ipAddress);

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteAsync(
        int id, string? currentUserId, string? currentUserName, string? ipAddress)
    {
        var item = await _context.FollowUps.FindAsync(id);
        if (item == null)
        {
            return (false, "Follow-up not found.");
        }

        var summary = item.Subject;
        _context.FollowUps.Remove(item);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Delete",
            "FollowUp",
            id.ToString(),
            summary,
            null,
            "Success",
            $"Deleted Follow-up: {summary}",
            ipAddress);

        return (true, null);
    }

    public async Task<int> GetPendingCountAsync(string? userId, string role)
    {
        var query = _context.FollowUps.Where(f => f.Status == FollowUpStatuses.Planned);
        if (role == UserRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
        {
            query = query.Where(f => f.AssignedTo == userId);
        }
        return await query.CountAsync();
    }

    public async Task<int> GetOverdueCountAsync(string? userId, string role)
    {
        var query = _context.FollowUps.Where(f => f.Status == FollowUpStatuses.Planned && f.FollowUpDate.Date < DateTime.Today);
        if (role == UserRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
        {
            query = query.Where(f => f.AssignedTo == userId);
        }
        return await query.CountAsync();
    }
}
