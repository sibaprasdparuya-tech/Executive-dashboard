using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public interface ILeadService
{
    Task<List<Lead>> GetAllAsync(string? userId, string role, string? searchTerm = null, string? status = null, string? assignedTo = null);
    Task<Lead?> GetByIdAsync(int id, string? userId, string role);
    Task<(bool Success, string? ErrorMessage, Lead? Lead)> CreateAsync(Lead lead, string? currentUserId, string? currentUserName, string? ipAddress);
    Task<(bool Success, string? ErrorMessage)> UpdateAsync(Lead lead, string? currentUserId, string? currentUserName, string? ipAddress);
    Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id, string? currentUserId, string? currentUserName, string? ipAddress);
    Task<(bool Success, string? ErrorMessage, int? CustomerId, int? OpportunityId)> ConvertLeadAsync(ConvertLeadDto dto, string? currentUserId, string? currentUserName, string? ipAddress);
}

public class LeadService : ILeadService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICustomerService _customerService;

    public LeadService(ApplicationDbContext context, IAuditService auditService, ICustomerService customerService)
    {
        _context = context;
        _auditService = auditService;
        _customerService = customerService;
    }

    public async Task<List<Lead>> GetAllAsync(string? userId, string role, string? searchTerm = null, string? status = null, string? assignedTo = null)
    {
        var query = _context.Leads
            .Include(l => l.AssignedUser)
            .Include(l => l.ConvertedCustomer)
            .AsNoTracking()
            .AsQueryable();

        if (role == UserRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
        {
            query = query.Where(l => l.AssignedTo == userId);
        }

        if (!string.IsNullOrWhiteSpace(assignedTo))
        {
            query = query.Where(l => l.AssignedTo == assignedTo);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(l => l.Status.ToLower() == status.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(l =>
                l.LeadName.ToLower().Contains(term) ||
                l.Email.ToLower().Contains(term) ||
                l.Phone.ToLower().Contains(term) ||
                (l.CompanyName != null && l.CompanyName.ToLower().Contains(term)));
        }

        return await query.OrderByDescending(l => l.CreatedDate).ToListAsync();
    }

    public async Task<Lead?> GetByIdAsync(int id, string? userId, string role)
    {
        var query = _context.Leads
            .Include(l => l.AssignedUser)
            .Include(l => l.ConvertedCustomer)
            .Include(l => l.ConvertedOpportunity)
            .Include(l => l.FollowUps)
            .Include(l => l.Activities)
            .AsQueryable();

        if (role == UserRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
        {
            query = query.Where(l => l.AssignedTo == userId);
        }

        return await query.FirstOrDefaultAsync(l => l.LeadId == id);
    }

    public async Task<(bool Success, string? ErrorMessage, Lead? Lead)> CreateAsync(
        Lead lead, string? currentUserId, string? currentUserName, string? ipAddress)
    {
        if (lead.ExpectedValue < 0)
        {
            return (false, "Expected Value must be greater than or equal to 0.", null);
        }

        if (!LeadStatuses.All.Contains(lead.Status))
        {
            return (false, "Invalid lead status.", null);
        }

        if (string.IsNullOrEmpty(lead.AssignedTo))
        {
            lead.AssignedTo = currentUserId;
        }

        lead.CreatedDate = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(lead.LeadCode))
        {
            var maxId = await _context.Leads.MaxAsync(l => (int?)l.LeadId) ?? 0;
            lead.LeadCode = $"LEAD-{(maxId + 1):D4}";
        }

        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Create",
            "Lead",
            lead.LeadId.ToString(),
            null,
            $"{lead.LeadName} ({lead.Status})",
            "Success",
            $"Created Lead: {lead.LeadName}, Code: {lead.LeadCode}",
            ipAddress);

        return (true, null, lead);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateAsync(
        Lead lead, string? currentUserId, string? currentUserName, string? ipAddress)
    {
        var existing = await _context.Leads.FindAsync(lead.LeadId);
        if (existing == null)
        {
            return (false, "Lead not found.");
        }

        if (lead.ExpectedValue < 0)
        {
            return (false, "Expected Value must be greater than or equal to 0.");
        }

        // Workflow transition rule: Once converted, status cannot be reverted directly to New
        if (existing.Status == LeadStatuses.Converted && lead.Status != LeadStatuses.Converted)
        {
            return (false, "Converted lead cannot have its status reverted.");
        }

        var oldSummary = $"{existing.LeadName}, Status: {existing.Status}, Value: {existing.ExpectedValue}";

        existing.LeadName = lead.LeadName;
        existing.Email = lead.Email;
        existing.Phone = lead.Phone;
        existing.CompanyName = lead.CompanyName;
        existing.Source = lead.Source;
        existing.Status = lead.Status;
        existing.Priority = lead.Priority;
        existing.ExpectedValue = lead.ExpectedValue;
        existing.Notes = lead.Notes;

        if (!string.IsNullOrEmpty(lead.AssignedTo))
        {
            existing.AssignedTo = lead.AssignedTo;
        }

        await _context.SaveChangesAsync();

        var newSummary = $"{existing.LeadName}, Status: {existing.Status}, Value: {existing.ExpectedValue}";

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Update",
            "Lead",
            existing.LeadId.ToString(),
            oldSummary,
            newSummary,
            "Success",
            $"Updated Lead: {existing.LeadName}",
            ipAddress);

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteAsync(
        int id, string? currentUserId, string? currentUserName, string? ipAddress)
    {
        var lead = await _context.Leads.FindAsync(id);
        if (lead == null)
        {
            return (false, "Lead not found.");
        }

        var summary = $"{lead.LeadName} ({lead.Email})";
        _context.Leads.Remove(lead);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Delete",
            "Lead",
            id.ToString(),
            summary,
            null,
            "Success",
            $"Deleted Lead: {summary}",
            ipAddress);

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage, int? CustomerId, int? OpportunityId)> ConvertLeadAsync(
        ConvertLeadDto dto, string? currentUserId, string? currentUserName, string? ipAddress)
    {
        var lead = await _context.Leads.FindAsync(dto.LeadId);
        if (lead == null)
        {
            return (false, "Lead not found.", null, null);
        }

        if (lead.Status == LeadStatuses.Converted)
        {
            return (false, "Lead has already been converted.", lead.ConvertedCustomerId, lead.ConvertedOpportunityId);
        }

        // 1. Create or Find Customer
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email.ToLower() == lead.Email.ToLower());
        if (customer == null)
        {
            customer = new Customer
            {
                CustomerName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Status = CustomerStatuses.Active,
                CreatedBy = lead.AssignedTo ?? currentUserId,
                CreatedDate = DateTime.UtcNow
            };

            var maxCustId = await _context.Customers.MaxAsync(c => (int?)c.CustomerId) ?? 0;
            customer.CustomerCode = $"CUST-{(maxCustId + 1):D4}";

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
        }

        // 2. Optionally create Opportunity
        Opportunity? opp = null;
        if (dto.CreateOpportunity)
        {
            var oppName = string.IsNullOrWhiteSpace(dto.OpportunityName)
                ? $"{lead.LeadName} - Opportunity"
                : dto.OpportunityName.Trim();

            var oppAmount = dto.OpportunityAmount > 0 ? dto.OpportunityAmount : (lead.ExpectedValue > 0 ? lead.ExpectedValue : 1000m);
            var closeDate = dto.ExpectedCloseDate ?? DateTime.Today.AddDays(30);

            opp = new Opportunity
            {
                OpportunityName = oppName,
                CustomerId = customer.CustomerId,
                LeadId = lead.LeadId,
                Amount = oppAmount,
                Stage = OpportunityStages.Qualification,
                Probability = 25,
                ExpectedCloseDate = closeDate,
                Status = OpportunityStatuses.Open,
                CreatedDate = DateTime.UtcNow,
                AssignedTo = lead.AssignedTo ?? currentUserId,
                Notes = dto.Notes ?? $"Created from conversion of Lead #{lead.LeadCode}"
            };

            _context.Opportunities.Add(opp);
            await _context.SaveChangesAsync();
        }

        // 3. Mark Lead as Converted
        lead.Status = LeadStatuses.Converted;
        lead.ConvertedCustomerId = customer.CustomerId;
        if (opp != null)
        {
            lead.ConvertedOpportunityId = opp.OpportunityId;
        }

        await _context.SaveChangesAsync();

        // 4. Record Conversion in Audit Log
        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Convert",
            "Lead",
            lead.LeadId.ToString(),
            $"Status: {LeadStatuses.Qualified}",
            $"Converted to Customer #{customer.CustomerId}" + (opp != null ? $", Opportunity #{opp.OpportunityId}" : ""),
            "Success",
            $"Converted Lead {lead.LeadName} to Customer {customer.CustomerName}",
            ipAddress);

        return (true, null, customer.CustomerId, opp?.OpportunityId);
    }
}
