using System.Security.Claims;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/leads")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = "Bearer,Identity.Application")]
public class LeadsApiController : ControllerBase
{
    private readonly ILeadService _leadService;
    private readonly UserManager<ApplicationUser> _userManager;

    public LeadsApiController(ILeadService leadService, UserManager<ApplicationUser> userManager)
    {
        _leadService = leadService;
        _userManager = userManager;
    }

    private string GetCurrentUserId() => _userManager?.GetUserId(User) ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string GetCurrentUserName() => User.Identity?.Name ?? "API User";
    private string GetCurrentRole() => User.IsInRole(UserRoles.Admin)
        ? UserRoles.Admin
        : (User.IsInRole(UserRoles.Manager) ? UserRoles.Manager : UserRoles.SalesExecutive);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LeadDto>>> GetLeads([FromQuery] string? search, [FromQuery] string? status)
    {
        var list = await _leadService.GetAllAsync(GetCurrentUserId(), GetCurrentRole(), search, status);
        var dtos = list.Select(l => new LeadDto
        {
            LeadId = l.LeadId,
            LeadCode = l.LeadCode,
            LeadName = l.LeadName,
            Email = l.Email,
            Phone = l.Phone,
            CompanyName = l.CompanyName,
            Source = l.Source,
            Status = l.Status,
            Priority = l.Priority,
            ExpectedValue = l.ExpectedValue,
            CreatedDate = l.CreatedDate,
            AssignedTo = l.AssignedTo,
            AssignedUserName = l.AssignedUser?.FullName ?? l.AssignedUser?.UserName,
            ConvertedCustomerId = l.ConvertedCustomerId,
            ConvertedOpportunityId = l.ConvertedOpportunityId,
            Notes = l.Notes
        });
        return Ok(dtos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LeadDto>> GetLead(int id)
    {
        var l = await _leadService.GetByIdAsync(id, GetCurrentUserId(), GetCurrentRole());
        if (l == null)
        {
            return NotFound(new { message = $"Lead with ID {id} not found." });
        }

        return Ok(new LeadDto
        {
            LeadId = l.LeadId,
            LeadCode = l.LeadCode,
            LeadName = l.LeadName,
            Email = l.Email,
            Phone = l.Phone,
            CompanyName = l.CompanyName,
            Source = l.Source,
            Status = l.Status,
            Priority = l.Priority,
            ExpectedValue = l.ExpectedValue,
            CreatedDate = l.CreatedDate,
            AssignedTo = l.AssignedTo,
            AssignedUserName = l.AssignedUser?.FullName ?? l.AssignedUser?.UserName,
            ConvertedCustomerId = l.ConvertedCustomerId,
            ConvertedOpportunityId = l.ConvertedOpportunityId,
            Notes = l.Notes
        });
    }

    [HttpPost]
    public async Task<ActionResult<LeadDto>> CreateLead([FromBody] LeadDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var lead = new Lead
        {
            LeadName = dto.LeadName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName,
            Source = string.IsNullOrWhiteSpace(dto.Source) ? LeadSources.Website : dto.Source,
            Status = string.IsNullOrWhiteSpace(dto.Status) ? LeadStatuses.New : dto.Status,
            Priority = string.IsNullOrWhiteSpace(dto.Priority) ? "Medium" : dto.Priority,
            ExpectedValue = dto.ExpectedValue,
            AssignedTo = GetCurrentUserId(),
            Notes = dto.Notes
        };

        var result = await _leadService.CreateAsync(lead, GetCurrentUserId(), GetCurrentUserName(), ip);
        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        dto.LeadId = lead.LeadId;
        dto.LeadCode = lead.LeadCode;
        dto.CreatedDate = lead.CreatedDate;
        dto.AssignedTo = lead.AssignedTo;

        return CreatedAtAction(nameof(GetLead), new { id = lead.LeadId }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateLead(int id, [FromBody] LeadDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var lead = new Lead
        {
            LeadId = id,
            LeadName = dto.LeadName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName,
            Source = dto.Source,
            Status = dto.Status,
            Priority = dto.Priority,
            ExpectedValue = dto.ExpectedValue,
            Notes = dto.Notes,
            AssignedTo = dto.AssignedTo
        };

        var result = await _leadService.UpdateAsync(lead, GetCurrentUserId(), GetCurrentUserName(), ip);
        if (!result.Success)
        {
            if (result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
            {
                return NotFound(new { message = result.ErrorMessage });
            }
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new { message = "Lead updated successfully." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteLead(int id)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _leadService.DeleteAsync(id, GetCurrentUserId(), GetCurrentUserName(), ip);

        if (!result.Success)
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        return Ok(new { message = "Lead deleted successfully." });
    }

    [HttpPost("convert")]
    public async Task<IActionResult> ConvertLead([FromBody] ConvertLeadDto dto)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _leadService.ConvertLeadAsync(dto, GetCurrentUserId(), GetCurrentUserName(), ip);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new
        {
            message = "Lead converted successfully.",
            customerId = result.CustomerId,
            opportunityId = result.OpportunityId
        });
    }
}
