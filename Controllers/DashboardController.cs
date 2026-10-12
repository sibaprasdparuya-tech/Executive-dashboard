using System.Security.Claims;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(IDashboardService dashboardService, UserManager<ApplicationUser> userManager)
    {
        _dashboardService = dashboardService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string dateFilter = "ThisMonth", DateTime? fromDate = null, DateTime? toDate = null)
    {
        var user = await _userManager.GetUserAsync(User);
        var userId = user?.Id;
        var role = User.IsInRole(UserRoles.Admin)
            ? UserRoles.Admin
            : (User.IsInRole(UserRoles.Manager) ? UserRoles.Manager : UserRoles.SalesExecutive);

        var model = await _dashboardService.GetDashboardDataAsync(userId, role, dateFilter, fromDate, toDate);
        ViewData["CurrentRole"] = role;
        ViewData["CurrentUser"] = user?.FullName ?? user?.UserName;

        return View(model);
    }
}
