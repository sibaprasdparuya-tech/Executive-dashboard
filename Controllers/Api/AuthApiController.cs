using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthApiController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAuditService _auditService;

    public AuthApiController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwtTokenService,
        IAuditService auditService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _auditService = auditService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new LoginResponseDto { Success = false, Message = "Invalid payload" });
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null)
        {
            await _auditService.LogAsync(null, request.Email, "Failed Login", "API Auth", null, null, null, "Failed", "API login invalid user", ip);
            return Unauthorized(new LoginResponseDto { Success = false, Message = "Invalid credentials." });
        }

        if (!user.IsActive)
        {
            await _auditService.LogAsync(user.Id, user.Email, "Failed Login", "API Auth", user.Id, null, null, "Failed", "Deactivated user API login attempt", ip);
            return StatusCode(403, new LoginResponseDto { Success = false, Message = "User account deactivated." });
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            await _auditService.LogAsync(user.Id, user.Email, "Failed Login", "API Auth", user.Id, null, null, "Failed", "Locked account API login attempt", ip);
            return StatusCode(423, new LoginResponseDto { Success = false, Message = "User account is locked out." });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            await _auditService.LogAsync(user.Id, user.Email, "Failed Login", "API Auth", user.Id, null, null, "Failed", "Invalid password API login attempt", ip);
            if (result.IsLockedOut)
            {
                return StatusCode(423, new LoginResponseDto { Success = false, Message = "Account is now locked out due to failed attempts." });
            }
            return Unauthorized(new LoginResponseDto { Success = false, Message = "Invalid credentials." });
        }

        // Reset failed count
        await _userManager.ResetAccessFailedCountAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? UserRoles.SalesExecutive;

        var token = _jwtTokenService.GenerateToken(user, primaryRole);

        await _auditService.LogAsync(user.Id, user.Email, "Login", "API Auth", user.Id, null, null, "Success", "API login successful token issued", ip);

        return Ok(new LoginResponseDto
        {
            Success = true,
            Message = "Authentication successful.",
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = primaryRole,
            Expiration = DateTime.UtcNow.AddHours(24)
        });
    }

    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = "Bearer,Identity.Application")]
    public async Task<IActionResult> Logout()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user != null)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _auditService.LogAsync(user.Id, user.Email, "Logout", "API Auth", user.Id, null, null, "Success", "API logout token terminated", ip);
        }
        await _signInManager.SignOutAsync();
        return Ok(new { message = "Logged out successfully." });
    }
}
