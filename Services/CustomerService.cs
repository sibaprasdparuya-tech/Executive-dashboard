using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public interface ICustomerService
{
    Task<List<Customer>> GetAllAsync(string? userId, string role, string? searchTerm = null, string? status = null);
    Task<Customer?> GetByIdAsync(int id, string? userId, string role);
    Task<(bool Success, string? ErrorMessage, Customer? Customer)> CreateAsync(Customer customer, string? currentUserId, string? currentUserName, string? ipAddress);
    Task<(bool Success, string? ErrorMessage)> UpdateAsync(Customer customer, string? currentUserId, string? currentUserName, string? ipAddress);
    Task<(bool Success, string? ErrorMessage)> DeleteAsync(int id, string? currentUserId, string? currentUserName, string? ipAddress);
    Task<bool> IsEmailUniqueAsync(string email, int? excludeCustomerId = null);
    Task<bool> IsPhoneUniqueAsync(string phone, int? excludeCustomerId = null);
}

public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public CustomerService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<List<Customer>> GetAllAsync(string? userId, string role, string? searchTerm = null, string? status = null)
    {
        var query = _context.Customers
            .Include(c => c.Owner)
            .AsNoTracking()
            .AsQueryable();

        // Role-based scoping
        if (role == UserRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
        {
            query = query.Where(c => c.CreatedBy == userId);
        }

        // Search filtering: Customer Name, Email, Phone, Company
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(c =>
                c.CustomerName.ToLower().Contains(term) ||
                c.Email.ToLower().Contains(term) ||
                c.Phone.ToLower().Contains(term) ||
                (c.CompanyName != null && c.CompanyName.ToLower().Contains(term)));
        }

        // Status filter
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status.ToLower() == status.ToLower());
        }

        return await query.OrderByDescending(c => c.CreatedDate).ToListAsync();
    }

    public async Task<Customer?> GetByIdAsync(int id, string? userId, string role)
    {
        var query = _context.Customers
            .Include(c => c.Owner)
            .Include(c => c.Opportunities)
            .Include(c => c.FollowUps)
            .Include(c => c.Activities)
            .AsQueryable();

        if (role == UserRoles.SalesExecutive && !string.IsNullOrEmpty(userId))
        {
            query = query.Where(c => c.CreatedBy == userId);
        }

        return await query.FirstOrDefaultAsync(c => c.CustomerId == id);
    }

    public async Task<bool> IsEmailUniqueAsync(string email, int? excludeCustomerId = null)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        var cleanEmail = email.Trim().ToLower();
        return !await _context.Customers.AnyAsync(c =>
            c.Email.ToLower() == cleanEmail &&
            (!excludeCustomerId.HasValue || c.CustomerId != excludeCustomerId.Value));
    }

    public async Task<bool> IsPhoneUniqueAsync(string phone, int? excludeCustomerId = null)
    {
        if (string.IsNullOrWhiteSpace(phone)) return false;
        var cleanPhone = phone.Trim();
        return !await _context.Customers.AnyAsync(c =>
            c.Phone == cleanPhone &&
            (!excludeCustomerId.HasValue || c.CustomerId != excludeCustomerId.Value));
    }

    public async Task<(bool Success, string? ErrorMessage, Customer? Customer)> CreateAsync(
        Customer customer, string? currentUserId, string? currentUserName, string? ipAddress)
    {
        // Business Validation: Email uniqueness
        if (!await IsEmailUniqueAsync(customer.Email))
        {
            return (false, "Email is already registered for another customer.", null);
        }

        // Business Validation: Phone uniqueness
        if (!await IsPhoneUniqueAsync(customer.Phone))
        {
            return (false, "Phone number is already registered for another customer.", null);
        }

        // Auto-assign owner if null
        if (string.IsNullOrEmpty(customer.CreatedBy))
        {
            customer.CreatedBy = currentUserId;
        }

        customer.CreatedDate = DateTime.UtcNow;

        // Auto generate CustomerCode if not set
        if (string.IsNullOrWhiteSpace(customer.CustomerCode))
        {
            var maxId = await _context.Customers.MaxAsync(c => (int?)c.CustomerId) ?? 0;
            customer.CustomerCode = $"CUST-{(maxId + 1):D4}";
        }

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        // Audit Logging
        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Create",
            "Customer",
            customer.CustomerId.ToString(),
            null,
            $"{customer.CustomerName} ({customer.Email})",
            "Success",
            $"Created Customer: {customer.CustomerName}, Code: {customer.CustomerCode}",
            ipAddress);

        return (true, null, customer);
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateAsync(
        Customer customer, string? currentUserId, string? currentUserName, string? ipAddress)
    {
        var existing = await _context.Customers.FindAsync(customer.CustomerId);
        if (existing == null)
        {
            return (false, "Customer not found.");
        }

        // Validate uniqueness excluding current
        if (!await IsEmailUniqueAsync(customer.Email, customer.CustomerId))
        {
            return (false, "Email is already registered for another customer.");
        }

        if (!await IsPhoneUniqueAsync(customer.Phone, customer.CustomerId))
        {
            return (false, "Phone number is already registered for another customer.");
        }

        var oldSummary = $"{existing.CustomerName}, {existing.Email}, {existing.Phone}, {existing.Status}";

        existing.CustomerName = customer.CustomerName;
        existing.Email = customer.Email;
        existing.Phone = customer.Phone;
        existing.CompanyName = customer.CompanyName;
        existing.Address = customer.Address;
        existing.City = customer.City;
        existing.State = customer.State;
        existing.Status = customer.Status;
        existing.ModifiedDate = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(customer.CreatedBy))
        {
            existing.CreatedBy = customer.CreatedBy;
        }

        await _context.SaveChangesAsync();

        var newSummary = $"{existing.CustomerName}, {existing.Email}, {existing.Phone}, {existing.Status}";

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Update",
            "Customer",
            existing.CustomerId.ToString(),
            oldSummary,
            newSummary,
            "Success",
            $"Updated Customer: {existing.CustomerName}",
            ipAddress);

        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteAsync(
        int id, string? currentUserId, string? currentUserName, string? ipAddress)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
        {
            return (false, "Customer not found.");
        }

        var summary = $"{customer.CustomerName} ({customer.Email})";
        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Delete",
            "Customer",
            id.ToString(),
            summary,
            null,
            "Success",
            $"Deleted Customer: {summary}",
            ipAddress);

        return (true, null);
    }
}
