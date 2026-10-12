using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _auditService = auditService;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        // Find user by email
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            await _auditService.LogAsync(
                null,
                model.Email,
                "Failed Login",
                "Auth",
                null,
                null,
                null,
                "Failed",
                $"Login attempt with non-existent email: {model.Email}",
                ipAddress);

            ModelState.AddModelError(string.Empty, "Invalid login credentials.");
            return View(model);
        }

        // Check if user is active
        if (!user.IsActive)
        {
            await _auditService.LogAsync(
                user.Id,
                user.Email,
                "Failed Login",
                "Auth",
                user.Id,
                null,
                null,
                "Failed",
                $"Deactivated user attempted login: {user.Email}",
                ipAddress);

            ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact an administrator.");
            return View(model);
        }

        // Check if locked out
        if (await _userManager.IsLockedOutAsync(user))
        {
            await _auditService.LogAsync(
                user.Id,
                user.Email,
                "Failed Login",
                "Auth",
                user.Id,
                null,
                null,
                "Failed",
                $"Login attempted on locked account: {user.Email}",
                ipAddress);

            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
            ModelState.AddModelError(string.Empty, $"Account is locked due to multiple failed login attempts until {lockoutEnd:HH:mm:ss UTC}.");
            return View(model);
        }

        // Attempt sign in with lockout enabled
        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            // Reset access failed count
            await _userManager.ResetAccessFailedCountAsync(user);

            await _auditService.LogAsync(
                user.Id,
                user.Email,
                "Login",
                "Auth",
                user.Id,
                null,
                null,
                "Success",
                $"User successfully logged in: {user.Email}",
                ipAddress);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Dashboard");
        }

        if (result.IsLockedOut)
        {
            await _auditService.LogAsync(
                user.Id,
                user.Email,
                "Lockout",
                "Security",
                user.Id,
                null,
                null,
                "Warning",
                $"Account locked out due to repeated failed attempts: {user.Email}",
                ipAddress);

            ModelState.AddModelError(string.Empty, "This account has been temporarily locked out due to multiple failed attempts.");
            return View(model);
        }

        // Record failed attempt
        await _auditService.LogAsync(
            user.Id,
            user.Email,
            "Failed Login",
            "Auth",
            user.Id,
            null,
            null,
            "Failed",
            $"Invalid password attempt for: {user.Email}. Failed count: {user.AccessFailedCount}",
            ipAddress);

        ModelState.AddModelError(string.Empty, "Invalid login credentials.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var user = await _userManager.GetUserAsync(User);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        await _signInManager.SignOutAsync();

        if (user != null)
        {
            await _auditService.LogAsync(
                user.Id,
                user.Email,
                "Logout",
                "Auth",
                user.Id,
                null,
                null,
                "Success",
                $"User logged out: {user.Email}",
                ipAddress);
        }

        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError(nameof(model.Email), "Email is already registered.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            EmailConfirmed = true,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            // Assign requested role (default SalesExecutive)
            var roleToAssign = string.IsNullOrEmpty(model.Role) ? UserRoles.SalesExecutive : model.Role;
            await _userManager.AddToRoleAsync(user, roleToAssign);

            await _auditService.LogAsync(
                user.Id,
                user.Email,
                "Register",
                "User",
                user.Id,
                null,
                $"Role: {roleToAssign}",
                "Success",
                $"New user registered: {user.Email} as {roleToAssign}",
                ipAddress);

            // Automatically sign in
            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Dashboard");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
