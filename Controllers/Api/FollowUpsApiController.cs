using System.Security.Claims;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/followups")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = "Bearer,Identity.Application")]
public class FollowUpsApiController : ControllerBase
{
    private readonly IFollowUpService _followUpService;
    private readonly UserManager<ApplicationUser> _userManager;

    public FollowUpsApiController(IFollowUpService followUpService, UserManager<ApplicationUser> userManager)
    {
        _followUpService = followUpService;
        _userManager = userManager;
    }

    private string GetCurrentUserId() => _userManager?.GetUserId(User) ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string GetCurrentUserName() => User.Identity?.Name ?? "API User";
    private string GetCurrentRole() => User.IsInRole(UserRoles.Admin)
        ? UserRoles.Admin
        : (User.IsInRole(UserRoles.Manager) ? UserRoles.Manager : UserRoles.SalesExecutive);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FollowUpDto>>> GetFollowUps([FromQuery] string? search, [FromQuery] string? status, [FromQuery] DateTime? date)
    {
        var list = await _followUpService.GetAllAsync(GetCurrentUserId(), GetCurrentRole(), search, status, date);
        var dtos = list.Select(f => new FollowUpDto
        {
            FollowUpId = f.FollowUpId,
            CustomerId = f.CustomerId,
            CustomerName = f.Customer?.CustomerName,
            LeadId = f.LeadId,
            LeadName = f.Lead?.LeadName,
            OpportunityId = f.OpportunityId,
            OpportunityName = f.Opportunity?.OpportunityName,
            RelatedType = f.RelatedType,
            RelatedId = f.RelatedId,
            FollowUpDate = f.FollowUpDate,
            FollowUpType = f.FollowUpType,
            Subject = f.Subject,
            Remarks = f.Remarks,
            Status = f.Status,
            AssignedTo = f.AssignedTo,
            AssignedUserName = f.AssignedUser?.FullName ?? f.AssignedUser?.UserName,
            Notes = f.Notes,
            CreatedDate = f.CreatedDate,
            IsOverdue = f.IsOverdue
        });
        return Ok(dtos);
    }

    [HttpPost]
    public async Task<ActionResult<FollowUpDto>> CreateFollowUp([FromBody] FollowUpDto dto)
    {
        if (dto.FollowUpDate.Date < DateTime.Today)
        {
            return BadRequest(new { message = "Follow-up date cannot be earlier than today." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var followUp = new FollowUp
        {
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            OpportunityId = dto.OpportunityId,
            FollowUpDate = dto.FollowUpDate,
            FollowUpType = string.IsNullOrWhiteSpace(dto.FollowUpType) ? FollowUpTypes.Call : dto.FollowUpType,
            Subject = dto.Subject,
            Remarks = dto.Remarks,
            Status = string.IsNullOrWhiteSpace(dto.Status) ? FollowUpStatuses.Planned : dto.Status,
            AssignedTo = GetCurrentUserId(),
            Notes = dto.Notes
        };

        var result = await _followUpService.CreateAsync(followUp, GetCurrentUserId(), GetCurrentUserName(), ip);
        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        dto.FollowUpId = followUp.FollowUpId;
        dto.CreatedDate = followUp.CreatedDate;
        dto.AssignedTo = followUp.AssignedTo;

        return CreatedAtAction(nameof(GetFollowUps), new { id = followUp.FollowUpId }, dto);
    }
}
