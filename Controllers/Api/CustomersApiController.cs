using System.Security.Claims;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/customers")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = "Bearer,Identity.Application")]
public class CustomersApiController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomersApiController(ICustomerService customerService, UserManager<ApplicationUser> userManager)
    {
        _customerService = customerService;
        _userManager = userManager;
    }

    private string GetCurrentUserId() => _userManager?.GetUserId(User) ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string GetCurrentUserName() => User.Identity?.Name ?? "API User";
    private string GetCurrentRole() => User.IsInRole(UserRoles.Admin)
        ? UserRoles.Admin
        : (User.IsInRole(UserRoles.Manager) ? UserRoles.Manager : UserRoles.SalesExecutive);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetCustomers([FromQuery] string? search, [FromQuery] string? status)
    {
        var list = await _customerService.GetAllAsync(GetCurrentUserId(), GetCurrentRole(), search, status);
        var dtos = list.Select(c => new CustomerDto
        {
            CustomerId = c.CustomerId,
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            Email = c.Email,
            Phone = c.Phone,
            CompanyName = c.CompanyName,
            Address = c.Address,
            City = c.City,
            State = c.State,
            Status = c.Status,
            CreatedDate = c.CreatedDate,
            CreatedBy = c.CreatedBy,
            OwnerName = c.Owner?.FullName ?? c.Owner?.UserName
        });
        return Ok(dtos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerDto>> GetCustomer(int id)
    {
        var c = await _customerService.GetByIdAsync(id, GetCurrentUserId(), GetCurrentRole());
        if (c == null)
        {
            return NotFound(new { message = $"Customer with ID {id} not found." });
        }

        return Ok(new CustomerDto
        {
            CustomerId = c.CustomerId,
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            Email = c.Email,
            Phone = c.Phone,
            CompanyName = c.CompanyName,
            Address = c.Address,
            City = c.City,
            State = c.State,
            Status = c.Status,
            CreatedDate = c.CreatedDate,
            CreatedBy = c.CreatedBy,
            OwnerName = c.Owner?.FullName ?? c.Owner?.UserName
        });
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> CreateCustomer([FromBody] CustomerDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var customer = new Customer
        {
            CustomerName = dto.CustomerName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName,
            Address = dto.Address,
            City = dto.City,
            State = dto.State,
            Status = string.IsNullOrWhiteSpace(dto.Status) ? CustomerStatuses.Active : dto.Status,
            CreatedBy = GetCurrentUserId()
        };

        var result = await _customerService.CreateAsync(customer, GetCurrentUserId(), GetCurrentUserName(), ip);
        if (!result.Success)
        {
            return Conflict(new { message = result.ErrorMessage });
        }

        dto.CustomerId = customer.CustomerId;
        dto.CustomerCode = customer.CustomerCode;
        dto.CreatedDate = customer.CreatedDate;
        dto.CreatedBy = customer.CreatedBy;

        return CreatedAtAction(nameof(GetCustomer), new { id = customer.CustomerId }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CustomerDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var customer = new Customer
        {
            CustomerId = id,
            CustomerName = dto.CustomerName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName,
            Address = dto.Address,
            City = dto.City,
            State = dto.State,
            Status = dto.Status,
            CreatedBy = dto.CreatedBy
        };

        var result = await _customerService.UpdateAsync(customer, GetCurrentUserId(), GetCurrentUserName(), ip);
        if (!result.Success)
        {
            if (result.ErrorMessage?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
            {
                return NotFound(new { message = result.ErrorMessage });
            }
            return Conflict(new { message = result.ErrorMessage });
        }

        return Ok(new { message = "Customer updated successfully." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _customerService.DeleteAsync(id, GetCurrentUserId(), GetCurrentUserName(), ip);

        if (!result.Success)
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        return Ok(new { message = "Customer deleted successfully." });
    }
}
