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
public class OpportunitiesController : Controller
{
    private readonly IOpportunityService _opportunityService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public OpportunitiesController(
        IOpportunityService opportunityService,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _opportunityService = opportunityService;
        _context = context;
        _userManager = userManager;
    }

    private string GetCurrentUserId() => _userManager.GetUserId(User) ?? string.Empty;
    private string GetCurrentUserName() => User.Identity?.Name ?? "User";
    private string GetCurrentRole() => User.IsInRole(UserRoles.Admin)
        ? UserRoles.Admin
        : (User.IsInRole(UserRoles.Manager) ? UserRoles.Manager : UserRoles.SalesExecutive);

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, string? stage, string? status, int page = 1)
    {
        var opps = await _opportunityService.GetAllAsync(GetCurrentUserId(), GetCurrentRole(), searchTerm, stage, status);
        ViewData["SearchTerm"] = searchTerm;
        ViewData["Stage"] = stage;
        ViewData["Status"] = status;

        const int pageSize = 10;
        var totalCount = opps.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

        var pagedItems = opps.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        ViewData["CurrentPage"] = page;
        ViewData["TotalPages"] = totalPages;
        ViewData["TotalCount"] = totalCount;
        ViewData["TotalAmount"] = opps.Where(o => o.Status == OpportunityStatuses.Open).Sum(o => o.Amount);
        ViewData["WeightedAmount"] = opps.Where(o => o.Status == OpportunityStatuses.Open).Sum(o => o.WeightedPipeline);

        return View(pagedItems);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var opp = await _opportunityService.GetByIdAsync(id, GetCurrentUserId(), GetCurrentRole());
        if (opp == null)
        {
            return NotFound();
        }
        return View(opp);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId, int? leadId)
    {
        await PopulateSelectListsAsync();
        return View(new Opportunity
        {
            CustomerId = customerId,
            LeadId = leadId,
            AssignedTo = GetCurrentUserId(),
            ExpectedCloseDate = DateTime.Today.AddDays(30)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Opportunity opportunity)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        // Server-side business validations (Section 5.3 & 17.7 & 17.19)
        if (opportunity.Amount <= 0)
        {
            ModelState.AddModelError(nameof(opportunity.Amount), "Opportunity Amount must be greater than 0.");
        }

        if (opportunity.Probability < 0 || opportunity.Probability > 100)
        {
            ModelState.AddModelError(nameof(opportunity.Probability), "Probability must be between 0 and 100.");
        }

        if (opportunity.Status == OpportunityStatuses.Open && opportunity.ExpectedCloseDate.Date < DateTime.Today)
        {
            ModelState.AddModelError(nameof(opportunity.ExpectedCloseDate), "Expected Close Date cannot be in the past.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateSelectListsAsync();
            return View(opportunity);
        }

        var result = await _opportunityService.CreateAsync(opportunity, GetCurrentUserId(), GetCurrentUserName(), ipAddress);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to create opportunity.");
            await PopulateSelectListsAsync();
            return View(opportunity);
        }

        TempData["SuccessMessage"] = $"Opportunity '{opportunity.OpportunityName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var opp = await _opportunityService.GetByIdAsync(id, GetCurrentUserId(), GetCurrentRole());
        if (opp == null)
        {
            return NotFound();
        }

        await PopulateSelectListsAsync();
        return View(opp);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Opportunity opportunity)
    {
        if (id != opportunity.OpportunityId)
        {
            return BadRequest();
        }

        if (opportunity.Amount <= 0)
        {
            ModelState.AddModelError(nameof(opportunity.Amount), "Opportunity Amount must be greater than 0.");
        }

        if (opportunity.Probability < 0 || opportunity.Probability > 100)
        {
            ModelState.AddModelError(nameof(opportunity.Probability), "Probability must be between 0 and 100.");
        }

        if (opportunity.Status == OpportunityStatuses.Open && opportunity.ExpectedCloseDate.Date < DateTime.Today)
        {
            ModelState.AddModelError(nameof(opportunity.ExpectedCloseDate), "Expected Close Date cannot be in the past.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateSelectListsAsync();
            return View(opportunity);
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _opportunityService.UpdateAsync(opportunity, GetCurrentUserId(), GetCurrentUserName(), ipAddress);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to update opportunity.");
            await PopulateSelectListsAsync();
            return View(opportunity);
        }

        TempData["SuccessMessage"] = $"Opportunity '{opportunity.OpportunityName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _opportunityService.DeleteAsync(id, GetCurrentUserId(), GetCurrentUserName(), ipAddress);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to delete opportunity.";
        }
        else
        {
            TempData["SuccessMessage"] = "Opportunity deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateSelectListsAsync()
    {
        ViewBag.Customers = new SelectList(await _context.Customers.OrderBy(c => c.CustomerName).ToListAsync(), "CustomerId", "CustomerName");
        ViewBag.Leads = new SelectList(await _context.Leads.OrderBy(l => l.LeadName).ToListAsync(), "LeadId", "LeadName");
        ViewBag.Users = new SelectList(await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync(), "Id", "FullName");
        ViewBag.Stages = new SelectList(OpportunityStages.All);
    }
}
