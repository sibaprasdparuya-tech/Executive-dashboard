using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly IReportService _reportService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsController(IReportService reportService, UserManager<ApplicationUser> userManager)
    {
        _reportService = reportService;
        _userManager = userManager;
    }

    private string GetCurrentUserId() => _userManager.GetUserId(User) ?? string.Empty;
    private string GetCurrentRole() => User.IsInRole(UserRoles.Admin)
        ? UserRoles.Admin
        : (User.IsInRole(UserRoles.Manager) ? UserRoles.Manager : UserRoles.SalesExecutive);

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    // 1. Customer Report
    [HttpGet]
    public async Task<IActionResult> CustomerReport(string? status, DateTime? fromDate, DateTime? toDate, string? export)
    {
        var data = await _reportService.GetCustomerReportAsync(GetCurrentUserId(), GetCurrentRole(), status, fromDate, toDate);

        if (export == "csv")
        {
            var csvBytes = _reportService.GenerateCsv(
                data,
                c => [c.CustomerCode, c.CustomerName, c.Email, c.Phone, c.CompanyName ?? "", c.Status, c.Owner?.FullName ?? "", c.CreatedDate.ToString("yyyy-MM-dd")],
                ["Customer Code", "Name", "Email", "Phone", "Company", "Status", "Owner", "Created Date"]
            );
            return File(csvBytes, "text/csv", $"Customer_Report_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
        }

        ViewData["Status"] = status;
        ViewData["FromDate"] = fromDate?.ToString("yyyy-MM-dd");
        ViewData["ToDate"] = toDate?.ToString("yyyy-MM-dd");

        return View(data);
    }

    // 2. Lead Report
    [HttpGet]
    public async Task<IActionResult> LeadReport(string? source, string? status, DateTime? fromDate, DateTime? toDate, string? export)
    {
        var data = await _reportService.GetLeadReportAsync(GetCurrentUserId(), GetCurrentRole(), source, status, fromDate, toDate);

        if (export == "csv")
        {
            var csvBytes = _reportService.GenerateCsv(
                data,
                l => [l.LeadCode, l.LeadName, l.Email, l.Phone, l.CompanyName ?? "", l.Source, l.Status, l.Priority, l.ExpectedValue.ToString("F2"), l.AssignedUser?.FullName ?? "", l.CreatedDate.ToString("yyyy-MM-dd")],
                ["Lead Code", "Name", "Email", "Phone", "Company", "Source", "Status", "Priority", "Expected Value ($)", "Assigned To", "Created Date"]
            );
            return File(csvBytes, "text/csv", $"Lead_Report_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
        }

        ViewData["Source"] = source;
        ViewData["Status"] = status;
        ViewData["FromDate"] = fromDate?.ToString("yyyy-MM-dd");
        ViewData["ToDate"] = toDate?.ToString("yyyy-MM-dd");

        return View(data);
    }

    // 3. Follow-Up Report
    [HttpGet]
    public async Task<IActionResult> FollowUpReport(string? status, DateTime? fromDate, DateTime? toDate, string? export)
    {
        var data = await _reportService.GetFollowUpReportAsync(GetCurrentUserId(), GetCurrentRole(), status, fromDate, toDate);

        if (export == "csv")
        {
            var csvBytes = _reportService.GenerateCsv(
                data,
                f => [f.FollowUpDate.ToString("yyyy-MM-dd"), f.Subject, f.FollowUpType, f.Status, f.Customer?.CustomerName ?? f.Lead?.LeadName ?? "N/A", f.AssignedUser?.FullName ?? "", f.Remarks ?? ""],
                ["Date", "Subject", "Type", "Status", "Related Entity", "Assigned User", "Remarks"]
            );
            return File(csvBytes, "text/csv", $"FollowUp_Report_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
        }

        ViewData["Status"] = status;
        ViewData["FromDate"] = fromDate?.ToString("yyyy-MM-dd");
        ViewData["ToDate"] = toDate?.ToString("yyyy-MM-dd");

        return View(data);
    }

    // 4. Opportunity Report
    [HttpGet]
    public async Task<IActionResult> OpportunityReport(string? stage, string? status, DateTime? fromDate, DateTime? toDate, string? export)
    {
        var data = await _reportService.GetOpportunityReportAsync(GetCurrentUserId(), GetCurrentRole(), stage, status, fromDate, toDate);

        if (export == "csv")
        {
            var csvBytes = _reportService.GenerateCsv(
                data,
                o => [o.OpportunityName, o.Customer?.CustomerName ?? o.Lead?.LeadName ?? "N/A", o.Stage, o.Amount.ToString("F2"), o.Probability.ToString() + "%", o.WeightedPipeline.ToString("F2"), o.ExpectedCloseDate.ToString("yyyy-MM-dd"), o.Status, o.AssignedUser?.FullName ?? ""],
                ["Opportunity Name", "Customer/Lead", "Stage", "Amount ($)", "Probability", "Weighted Pipeline ($)", "Expected Close Date", "Status", "Assigned User"]
            );
            return File(csvBytes, "text/csv", $"Opportunity_Report_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
        }

        ViewData["Stage"] = stage;
        ViewData["Status"] = status;
        ViewData["FromDate"] = fromDate?.ToString("yyyy-MM-dd");
        ViewData["ToDate"] = toDate?.ToString("yyyy-MM-dd");

        return View(data);
    }

    // 5. Pipeline Report
    [HttpGet]
    public async Task<IActionResult> PipelineReport(string? export)
    {
        var data = await _reportService.GetPipelineReportAsync(GetCurrentUserId(), GetCurrentRole());

        if (export == "csv")
        {
            var csvBytes = _reportService.GenerateCsv(
                data.ByStage,
                s => [s.Stage, s.Count.ToString(), s.TotalAmount.ToString("F2"), s.WeightedAmount.ToString("F2")],
                ["Stage", "Deals Count", "Total Amount ($)", "Weighted Amount ($)"]
            );
            return File(csvBytes, "text/csv", $"Pipeline_Report_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
        }

        return View(data);
    }

    // 6. Conversion / Sales Report
    [HttpGet]
    public async Task<IActionResult> ConversionReport()
    {
        var data = await _reportService.GetConversionReportAsync(GetCurrentUserId(), GetCurrentRole());
        return View(data);
    }

    // 7. User Activity Report (Admin/Manager)
    [HttpGet]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Manager}")]
    public async Task<IActionResult> UserActivityReport(DateTime? fromDate, DateTime? toDate, string? export)
    {
        var data = await _reportService.GetUserActivityReportAsync(fromDate, toDate);

        if (export == "csv")
        {
            var csvBytes = _reportService.GenerateCsv(
                data,
                u => [u.UserName, u.LoginCount.ToString(), u.CreateCount.ToString(), u.UpdateCount.ToString(), u.DeleteCount.ToString(), u.TotalActions.ToString(), u.LastActive?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"],
                ["User Name", "Logins", "Creates", "Updates", "Deletes", "Total Actions", "Last Active"]
            );
            return File(csvBytes, "text/csv", $"User_Activity_Report_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
        }

        ViewData["FromDate"] = fromDate?.ToString("yyyy-MM-dd");
        ViewData["ToDate"] = toDate?.ToString("yyyy-MM-dd");

        return View(data);
    }

    // 8. Audit Report (Admin/Manager)
    [HttpGet]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Manager}")]
    public async Task<IActionResult> AuditReport(string? userId, string? entityName, string? action, DateTime? fromDate, DateTime? toDate, string? export)
    {
        var data = await _reportService.GetAuditReportAsync(userId, entityName, action, fromDate, toDate);

        if (export == "csv")
        {
            var csvBytes = _reportService.GenerateCsv(
                data,
                a => [a.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"), a.UserName ?? "", a.Action, a.EntityName, a.RecordId ?? "", a.Result, a.Details ?? "", a.IpAddress ?? ""],
                ["Timestamp", "User", "Action", "Module", "Record ID", "Result", "Details", "IP Address"]
            );
            return File(csvBytes, "text/csv", $"Audit_Report_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
        }

        ViewData["UserId"] = userId;
        ViewData["EntityName"] = entityName;
        ViewData["ActionName"] = action;
        ViewData["FromDate"] = fromDate?.ToString("yyyy-MM-dd");
        ViewData["ToDate"] = toDate?.ToString("yyyy-MM-dd");

        return View(data);
    }
}
