using System.Security.Claims;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/opportunities")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = "Bearer,Identity.Application")]
public class OpportunitiesApiController : ControllerBase
{
    private readonly IOpportunityService _opportunityService;
    private readonly UserManager<ApplicationUser> _userManager;

    public OpportunitiesApiController(IOpportunityService opportunityService, UserManager<ApplicationUser> userManager)
    {
        _opportunityService = opportunityService;
        _userManager = userManager;
    }

    private string GetCurrentUserId() => _userManager?.GetUserId(User) ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string GetCurrentUserName() => User.Identity?.Name ?? "API User";
    private string GetCurrentRole() => User.IsInRole(UserRoles.Admin)
        ? UserRoles.Admin
        : (User.IsInRole(UserRoles.Manager) ? UserRoles.Manager : UserRoles.SalesExecutive);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OpportunityDto>>> GetOpportunities([FromQuery] string? search, [FromQuery] string? stage, [FromQuery] string? status)
    {
        var list = await _opportunityService.GetAllAsync(GetCurrentUserId(), GetCurrentRole(), search, stage, status);
        var dtos = list.Select(o => new OpportunityDto
        {
            OpportunityId = o.OpportunityId,
            OpportunityName = o.OpportunityName,
            CustomerId = o.CustomerId,
            CustomerName = o.Customer?.CustomerName,
            LeadId = o.LeadId,
            LeadName = o.Lead?.LeadName,
            Amount = o.Amount,
            Stage = o.Stage,
            Probability = o.Probability,
            ExpectedCloseDate = o.ExpectedCloseDate,
            Status = o.Status,
            CreatedDate = o.CreatedDate,
            AssignedTo = o.AssignedTo,
            AssignedUserName = o.AssignedUser?.FullName ?? o.AssignedUser?.UserName,
            Notes = o.Notes,
            WeightedPipeline = o.WeightedPipeline
        });
        return Ok(dtos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OpportunityDto>> GetOpportunity(int id)
    {
        var o = await _opportunityService.GetByIdAsync(id, GetCurrentUserId(), GetCurrentRole());
        if (o == null)
        {
            return NotFound(new { message = $"Opportunity with ID {id} not found." });
        }

        return Ok(new OpportunityDto
        {
            OpportunityId = o.OpportunityId,
            OpportunityName = o.OpportunityName,
            CustomerId = o.CustomerId,
            CustomerName = o.Customer?.CustomerName,
            LeadId = o.LeadId,
            LeadName = o.Lead?.LeadName,
            Amount = o.Amount,
            Stage = o.Stage,
            Probability = o.Probability,
            ExpectedCloseDate = o.ExpectedCloseDate,
            Status = o.Status,
            CreatedDate = o.CreatedDate,
            AssignedTo = o.AssignedTo,
            AssignedUserName = o.AssignedUser?.FullName ?? o.AssignedUser?.UserName,
            Notes = o.Notes,
            WeightedPipeline = o.WeightedPipeline
        });
    }

    [HttpPost]
    public async Task<ActionResult<OpportunityDto>> CreateOpportunity([FromBody] OpportunityDto dto)
    {
        // Business Validation 1: Amount > 0
        if (dto.Amount <= 0)
        {
            return BadRequest(new { message = "Opportunity Amount must be greater than 0." });
        }

        // Business Validation 2: Probability 0-100
        if (dto.Probability < 0 || dto.Probability > 100)
        {
            return BadRequest(new { message = "Probability must be between 0 and 100." });
        }

        // Business Validation 3: Close Date not in past for open
        if (dto.ExpectedCloseDate.Date < DateTime.Today)
        {
            return BadRequest(new { message = "Expected Close Date cannot be in the past." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var opp = new Opportunity
        {
            OpportunityName = dto.OpportunityName,
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            Amount = dto.Amount,
            Stage = string.IsNullOrWhiteSpace(dto.Stage) ? OpportunityStages.Qualification : dto.Stage,
            Probability = dto.Probability,
            ExpectedCloseDate = dto.ExpectedCloseDate,
            Status = string.IsNullOrWhiteSpace(dto.Status) ? OpportunityStatuses.Open : dto.Status,
            AssignedTo = GetCurrentUserId(),
            Notes = dto.Notes
        };

        var result = await _opportunityService.CreateAsync(opp, GetCurrentUserId(), GetCurrentUserName(), ip);
        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        dto.OpportunityId = opp.OpportunityId;
        dto.CreatedDate = opp.CreatedDate;
        dto.AssignedTo = opp.AssignedTo;
        dto.WeightedPipeline = opp.WeightedPipeline;

        return CreatedAtAction(nameof(GetOpportunity), new { id = opp.OpportunityId }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateOpportunity(int id, [FromBody] OpportunityDto dto)
    {
        if (dto.Amount <= 0)
        {
            return BadRequest(new { message = "Opportunity Amount must be greater than 0." });
        }

        if (dto.Probability < 0 || dto.Probability > 100)
        {
            return BadRequest(new { message = "Probability must be between 0 and 100." });
        }

        if (dto.ExpectedCloseDate.Date < DateTime.Today)
        {
            return BadRequest(new { message = "Expected Close Date cannot be in the past." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var opp = new Opportunity
        {
            OpportunityId = id,
            OpportunityName = dto.OpportunityName,
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            Amount = dto.Amount,
            Stage = dto.Stage,
            Probability = dto.Probability,
            ExpectedCloseDate = dto.ExpectedCloseDate,
            Status = dto.Status,
            Notes = dto.Notes,
            AssignedTo = dto.AssignedTo
        };

        var result = await _opportunityService.UpdateAsync(opp, GetCurrentUserId(), GetCurrentUserName(), ip);
        if (!result.Success)
        {
            if (result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
            {
                return NotFound(new { message = result.ErrorMessage });
            }
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(new { message = "Opportunity updated successfully." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteOpportunity(int id)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _opportunityService.DeleteAsync(id, GetCurrentUserId(), GetCurrentUserName(), ip);

        if (!result.Success)
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        return Ok(new { message = "Opportunity deleted successfully." });
    }
}
