using System.Security.Claims;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize]
public class ActivitiesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ActivitiesController(
        ApplicationDbContext context,
        IAuditService auditService,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _auditService = auditService;
        _userManager = userManager;
    }

    private string GetCurrentUserId() => _userManager.GetUserId(User) ?? string.Empty;
    private string GetCurrentUserName() => User.Identity?.Name ?? "User";
    private string GetCurrentRole() => User.IsInRole(UserRoles.Admin)
        ? UserRoles.Admin
        : (User.IsInRole(UserRoles.Manager) ? UserRoles.Manager : UserRoles.SalesExecutive);

    [HttpGet]
    public async Task<IActionResult> Index(string? activityType, string? status, DateTime? date, int page = 1)
    {
        var query = _context.Activities
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .Include(a => a.Opportunity)
            .Include(a => a.AssignedUser)
            .AsNoTracking().AsQueryable();

        var role = GetCurrentRole();
        var userId = GetCurrentUserId();

        if (role == UserRoles.SalesExecutive)
        {
            query = query.Where(a => a.AssignedTo == userId);
        }

        if (!string.IsNullOrWhiteSpace(activityType))
        {
            query = query.Where(a => a.ActivityType.ToLower() == activityType.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(a => a.Status.ToLower() == status.ToLower());
        }

        if (date.HasValue)
        {
            query = query.Where(a => a.ActivityDate.Date == date.Value.Date);
        }

        var totalCount = await query.CountAsync();
        const int pageSize = 10;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

        var items = await query.OrderByDescending(a => a.ActivityDate).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        ViewData["ActivityType"] = activityType;
        ViewData["Status"] = status;
        ViewData["Date"] = date?.ToString("yyyy-MM-dd");
        ViewData["CurrentPage"] = page;
        ViewData["TotalPages"] = totalPages;
        ViewData["TotalCount"] = totalCount;

        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId, int? leadId, int? opportunityId)
    {
        await PopulateSelectListsAsync();
        return View(new Activity
        {
            CustomerId = customerId,
            LeadId = leadId,
            OpportunityId = opportunityId,
            AssignedTo = GetCurrentUserId(),
            ActivityDate = DateTime.UtcNow
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Activity activity)
    {
        if (string.IsNullOrEmpty(activity.AssignedTo))
        {
            activity.AssignedTo = GetCurrentUserId();
        }

        if (!ModelState.IsValid)
        {
            await PopulateSelectListsAsync();
            return View(activity);
        }

        activity.CreatedDate = DateTime.UtcNow;
        _context.Activities.Add(activity);
        await _context.SaveChangesAsync();

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _auditService.LogAsync(
            GetCurrentUserId(),
            GetCurrentUserName(),
            "Create",
            "Activity",
            activity.ActivityId.ToString(),
            null,
            $"{activity.ActivityType}: {activity.Subject}",
            "Success",
            $"Logged activity {activity.Subject}",
            ipAddress);

        TempData["SuccessMessage"] = "Activity logged successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateSelectListsAsync()
    {
        ViewBag.Customers = new SelectList(await _context.Customers.OrderBy(c => c.CustomerName).ToListAsync(), "CustomerId", "CustomerName");
        ViewBag.Leads = new SelectList(await _context.Leads.OrderBy(l => l.LeadName).ToListAsync(), "LeadId", "LeadName");
        ViewBag.Opportunities = new SelectList(await _context.Opportunities.OrderBy(o => o.OpportunityName).ToListAsync(), "OpportunityId", "OpportunityName");
        ViewBag.Users = new SelectList(await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync(), "Id", "FullName");
        ViewBag.Types = new SelectList(ActivityTypes.All);
    }
}
