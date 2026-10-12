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
public class FollowUpsController : Controller
{
    private readonly IFollowUpService _followUpService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public FollowUpsController(
        IFollowUpService followUpService,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _followUpService = followUpService;
        _context = context;
        _userManager = userManager;
    }

    private string GetCurrentUserId() => _userManager.GetUserId(User) ?? string.Empty;
    private string GetCurrentUserName() => User.Identity?.Name ?? "User";
    private string GetCurrentRole() => User.IsInRole(UserRoles.Admin)
        ? UserRoles.Admin
        : (User.IsInRole(UserRoles.Manager) ? UserRoles.Manager : UserRoles.SalesExecutive);

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, string? status, DateTime? date, bool? overdueOnly, int page = 1)
    {
        var list = await _followUpService.GetAllAsync(GetCurrentUserId(), GetCurrentRole(), searchTerm, status, date, overdueOnly);
        ViewData["SearchTerm"] = searchTerm;
        ViewData["Status"] = status;
        ViewData["Date"] = date?.ToString("yyyy-MM-dd");
        ViewData["OverdueOnly"] = overdueOnly;

        const int pageSize = 10;
        var totalCount = list.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

        var pagedItems = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        ViewData["CurrentPage"] = page;
        ViewData["TotalPages"] = totalPages;
        ViewData["TotalCount"] = totalCount;
        ViewData["PendingCount"] = await _followUpService.GetPendingCountAsync(GetCurrentUserId(), GetCurrentRole());
        ViewData["OverdueCount"] = await _followUpService.GetOverdueCountAsync(GetCurrentUserId(), GetCurrentRole());

        return View(pagedItems);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var item = await _followUpService.GetByIdAsync(id, GetCurrentUserId(), GetCurrentRole());
        if (item == null)
        {
            return NotFound();
        }
        return View(item);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId, int? leadId, int? opportunityId)
    {
        await PopulateSelectListsAsync();
        return View(new FollowUp
        {
            CustomerId = customerId,
            LeadId = leadId,
            OpportunityId = opportunityId,
            AssignedTo = GetCurrentUserId(),
            FollowUpDate = DateTime.Today
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FollowUp followUp)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        // Business Validation: Follow-up date cannot be earlier than today for new/planned
        if (followUp.Status == FollowUpStatuses.Planned && followUp.FollowUpDate.Date < DateTime.Today)
        {
            ModelState.AddModelError(nameof(followUp.FollowUpDate), "Follow-up date cannot be earlier than today.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateSelectListsAsync();
            return View(followUp);
        }

        var result = await _followUpService.CreateAsync(followUp, GetCurrentUserId(), GetCurrentUserName(), ipAddress);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to create follow-up.");
            await PopulateSelectListsAsync();
            return View(followUp);
        }

        TempData["SuccessMessage"] = $"Follow-up '{followUp.Subject}' scheduled successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _followUpService.GetByIdAsync(id, GetCurrentUserId(), GetCurrentRole());
        if (item == null)
        {
            return NotFound();
        }

        await PopulateSelectListsAsync();
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FollowUp followUp)
    {
        if (id != followUp.FollowUpId)
        {
            return BadRequest();
        }

        if (followUp.Status == FollowUpStatuses.Planned && followUp.FollowUpDate.Date < DateTime.Today)
        {
            ModelState.AddModelError(nameof(followUp.FollowUpDate), "Follow-up date cannot be earlier than today.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateSelectListsAsync();
            return View(followUp);
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _followUpService.UpdateAsync(followUp, GetCurrentUserId(), GetCurrentUserName(), ipAddress);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to update follow-up.");
            await PopulateSelectListsAsync();
            return View(followUp);
        }

        TempData["SuccessMessage"] = $"Follow-up '{followUp.Subject}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, string? remarks)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _followUpService.CompleteFollowUpAsync(id, remarks, GetCurrentUserId(), GetCurrentUserName(), ipAddress);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to complete follow-up.";
        }
        else
        {
            TempData["SuccessMessage"] = "Follow-up marked as Completed and logged to activities.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reschedule(int id, DateTime newDate, string? remarks)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _followUpService.RescheduleFollowUpAsync(id, newDate, remarks, GetCurrentUserId(), GetCurrentUserName(), ipAddress);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to reschedule follow-up.";
        }
        else
        {
            TempData["SuccessMessage"] = $"Follow-up rescheduled to {newDate:yyyy-MM-dd}.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _followUpService.DeleteAsync(id, GetCurrentUserId(), GetCurrentUserName(), ipAddress);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to delete follow-up.";
        }
        else
        {
            TempData["SuccessMessage"] = "Follow-up deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateSelectListsAsync()
    {
        ViewBag.Customers = new SelectList(await _context.Customers.OrderBy(c => c.CustomerName).ToListAsync(), "CustomerId", "CustomerName");
        ViewBag.Leads = new SelectList(await _context.Leads.OrderBy(l => l.LeadName).ToListAsync(), "LeadId", "LeadName");
        ViewBag.Opportunities = new SelectList(await _context.Opportunities.OrderBy(o => o.OpportunityName).ToListAsync(), "OpportunityId", "OpportunityName");
        ViewBag.Users = new SelectList(await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync(), "Id", "FullName");
        ViewBag.Types = new SelectList(FollowUpTypes.All);
        ViewBag.Statuses = new SelectList(FollowUpStatuses.All);
    }
}
