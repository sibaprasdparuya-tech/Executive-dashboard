using System.Security.Claims;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/reports")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = "Bearer,Identity.Application")]
public class ReportsApiController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsApiController(IReportService reportService, UserManager<ApplicationUser> userManager)
    {
        _reportService = reportService;
        _userManager = userManager;
    }

    private string GetCurrentUserId() => _userManager?.GetUserId(User) ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string GetCurrentRole() => User.IsInRole(UserRoles.Admin)
        ? UserRoles.Admin
        : (User.IsInRole(UserRoles.Manager) ? UserRoles.Manager : UserRoles.SalesExecutive);

    [HttpGet("pipeline")]
    public async Task<ActionResult<PipelineReportDto>> GetPipelineReport()
    {
        var report = await _reportService.GetPipelineReportAsync(GetCurrentUserId(), GetCurrentRole());
        return Ok(report);
    }
}
