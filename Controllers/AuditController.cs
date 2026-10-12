using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Manager}")]
public class AuditController : Controller
{
    private readonly IAuditService _auditService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuditController(IAuditService auditService, UserManager<ApplicationUser> userManager)
    {
        _auditService = auditService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? userId, string? entityName, string? action, DateTime? fromDate, DateTime? toDate, int page = 1)
    {
        var logs = await _auditService.GetLogsAsync(userId, entityName, action, fromDate, toDate);

        ViewData["UserId"] = userId;
        ViewData["EntityName"] = entityName;
        ViewData["ActionName"] = action;
        ViewData["FromDate"] = fromDate?.ToString("yyyy-MM-dd");
        ViewData["ToDate"] = toDate?.ToString("yyyy-MM-dd");

        ViewBag.Users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
        ViewBag.Entities = new[] { "Auth", "Customer", "Lead", "Opportunity", "FollowUp", "Activity", "User", "Security" };
        ViewBag.Actions = new[] { "Login", "Failed Login", "Logout", "Create", "Update", "Delete", "Convert", "Role Change", "Security", "Complete", "Reschedule" };

        const int pageSize = 15;
        var totalCount = logs.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

        var pagedItems = logs.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        ViewData["CurrentPage"] = page;
        ViewData["TotalPages"] = totalPages;
        ViewData["TotalCount"] = totalCount;

        return View(pagedItems);
    }
}
