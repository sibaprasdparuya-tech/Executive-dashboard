using System.Security.Claims;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

[Authorize]
public class LeadsController : Controller
{
    private readonly ILeadService _leadService;
    private readonly UserManager<ApplicationUser> _userManager;

    public LeadsController(ILeadService leadService, UserManager<ApplicationUser> userManager)
    {
        _leadService = leadService;
        _userManager = userManager;
    }

    private string GetCurrentUserId() => _userManager.GetUserId(User) ?? string.Empty;
    private string GetCurrentUserName() => User.Identity?.Name ?? "User";
    private string GetCurrentRole() => User.IsInRole(UserRoles.Admin)
        ? UserRoles.Admin
        : (User.IsInRole(UserRoles.Manager) ? UserRoles.Manager : UserRoles.SalesExecutive);

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, string? status, string? assignedTo, int page = 1)
    {
        var leads = await _leadService.GetAllAsync(GetCurrentUserId(), GetCurrentRole(), searchTerm, status, assignedTo);
        ViewData["SearchTerm"] = searchTerm;
        ViewData["Status"] = status;
        ViewData["AssignedTo"] = assignedTo;
        ViewBag.Users = _userManager.Users.Where(u => u.IsActive).ToList();

        const int pageSize = 10;
        var totalCount = leads.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

        var pagedItems = leads.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        ViewData["CurrentPage"] = page;
        ViewData["TotalPages"] = totalPages;
        ViewData["TotalCount"] = totalCount;

        return View(pagedItems);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var lead = await _leadService.GetByIdAsync(id, GetCurrentUserId(), GetCurrentRole());
        if (lead == null)
        {
            return NotFound();
        }
        return View(lead);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.Users = _userManager.Users.Where(u => u.IsActive).ToList();
        return View(new Lead { AssignedTo = GetCurrentUserId() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Lead lead)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (!ModelState.IsValid)
        {
            ViewBag.Users = _userManager.Users.Where(u => u.IsActive).ToList();
            return View(lead);
        }

        var result = await _leadService.CreateAsync(lead, GetCurrentUserId(), GetCurrentUserName(), ipAddress);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to create lead.");
            ViewBag.Users = _userManager.Users.Where(u => u.IsActive).ToList();
            return View(lead);
        }

        TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' created successfully with code {lead.LeadCode}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var lead = await _leadService.GetByIdAsync(id, GetCurrentUserId(), GetCurrentRole());
        if (lead == null)
        {
            return NotFound();
        }

        ViewBag.Users = _userManager.Users.Where(u => u.IsActive).ToList();
        return View(lead);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Lead lead)
    {
        if (id != lead.LeadId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Users = _userManager.Users.Where(u => u.IsActive).ToList();
            return View(lead);
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _leadService.UpdateAsync(lead, GetCurrentUserId(), GetCurrentUserName(), ipAddress);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to update lead.");
            ViewBag.Users = _userManager.Users.Where(u => u.IsActive).ToList();
            return View(lead);
        }

        TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(ConvertLeadDto model)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _leadService.ConvertLeadAsync(model, GetCurrentUserId(), GetCurrentUserName(), ipAddress);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to convert lead.";
        }
        else
        {
            TempData["SuccessMessage"] = "Lead converted successfully to Customer and Opportunity!";
        }

        return RedirectToAction(nameof(Details), new { id = model.LeadId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _leadService.DeleteAsync(id, GetCurrentUserId(), GetCurrentUserName(), ipAddress);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to delete lead.";
        }
        else
        {
            TempData["SuccessMessage"] = "Lead deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}
