using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = UserRoles.Admin)]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAuditService _auditService;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IAuditService auditService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _auditService = auditService;
    }

    private string GetCurrentUserId() => _userManager.GetUserId(User) ?? string.Empty;
    private string GetCurrentUserName() => User.Identity?.Name ?? "Admin";

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, string? roleFilter)
    {
        var usersQuery = _userManager.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            usersQuery = usersQuery.Where(u =>
                u.FullName.ToLower().Contains(term) ||
                (u.Email != null && u.Email.ToLower().Contains(term)));
        }

        var users = await usersQuery.OrderByDescending(u => u.CreatedDate).ToListAsync();
        var model = new List<UserListItemViewModel>();

        foreach (var user in users)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            var primaryRole = userRoles.FirstOrDefault() ?? "None";

            if (!string.IsNullOrWhiteSpace(roleFilter) && primaryRole != roleFilter)
            {
                continue;
            }

            var isLocked = await _userManager.IsLockedOutAsync(user);

            model.Add(new UserListItemViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = primaryRole,
                IsActive = user.IsActive,
                AccessFailedCount = user.AccessFailedCount,
                IsLockedOut = isLocked,
                LockoutEnd = user.LockoutEnd,
                CreatedDate = user.CreatedDate
            });
        }

        ViewData["SearchTerm"] = searchTerm;
        ViewData["RoleFilter"] = roleFilter;
        ViewBag.Roles = new[] { UserRoles.Admin, UserRoles.Manager, UserRoles.SalesExecutive };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.Roles = new SelectList(new[] { UserRoles.Admin, UserRoles.Manager, UserRoles.SalesExecutive });
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = new SelectList(new[] { UserRoles.Admin, UserRoles.Manager, UserRoles.SalesExecutive });
            return View(model);
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError(nameof(model.Email), "Email is already registered.");
            ViewBag.Roles = new SelectList(new[] { UserRoles.Admin, UserRoles.Manager, UserRoles.SalesExecutive });
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
            await _userManager.AddToRoleAsync(user, model.Role);

            await _auditService.LogAsync(
                GetCurrentUserId(),
                GetCurrentUserName(),
                "Create",
                "User",
                user.Id,
                null,
                $"User: {user.Email}, Role: {model.Role}",
                "Success",
                $"Admin created user {user.Email} with role {model.Role}",
                ipAddress);

            TempData["SuccessMessage"] = $"User '{user.Email}' created successfully with role {model.Role}.";
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        ViewBag.Roles = new SelectList(new[] { UserRoles.Admin, UserRoles.Manager, UserRoles.SalesExecutive });
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var currentRole = roles.FirstOrDefault() ?? UserRoles.SalesExecutive;

        var model = new UserEditViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = currentRole,
            IsActive = user.IsActive
        };

        ViewBag.Roles = new SelectList(new[] { UserRoles.Admin, UserRoles.Manager, UserRoles.SalesExecutive }, currentRole);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = new SelectList(new[] { UserRoles.Admin, UserRoles.Manager, UserRoles.SalesExecutive }, model.Role);
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null)
        {
            return NotFound();
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var oldRoles = await _userManager.GetRolesAsync(user);
        var oldRole = oldRoles.FirstOrDefault() ?? "None";

        user.FullName = model.FullName;
        user.Email = model.Email;
        user.UserName = model.Email;
        user.IsActive = model.IsActive;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var err in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, err.Description);
            }
            ViewBag.Roles = new SelectList(new[] { UserRoles.Admin, UserRoles.Manager, UserRoles.SalesExecutive }, model.Role);
            return View(model);
        }

        if (oldRole != model.Role)
        {
            await _userManager.RemoveFromRolesAsync(user, oldRoles);
            await _userManager.AddToRoleAsync(user, model.Role);

            await _auditService.LogAsync(
                GetCurrentUserId(),
                GetCurrentUserName(),
                "Role Change",
                "User",
                user.Id,
                oldRole,
                model.Role,
                "Success",
                $"Admin updated role for user {user.Email} from {oldRole} to {model.Role}",
                ipAddress);
        }

        await _auditService.LogAsync(
            GetCurrentUserId(),
            GetCurrentUserName(),
            "Update",
            "User",
            user.Id,
            null,
            $"{user.FullName} ({user.Email})",
            "Success",
            $"Admin updated user profile for {user.Email}",
            ipAddress);

        TempData["SuccessMessage"] = $"User '{user.Email}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _auditService.LogAsync(
            GetCurrentUserId(),
            GetCurrentUserName(),
            "Security",
            "User",
            user.Id,
            (!user.IsActive ? "Active" : "Inactive"),
            (user.IsActive ? "Active" : "Inactive"),
            "Success",
            $"Admin {(user.IsActive ? "activated" : "deactivated")} user account {user.Email}",
            ipAddress);

        TempData["SuccessMessage"] = $"User '{user.Email}' is now {(user.IsActive ? "Active" : "Inactive")}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _auditService.LogAsync(
            GetCurrentUserId(),
            GetCurrentUserName(),
            "Security",
            "User",
            user.Id,
            "Locked",
            "Unlocked",
            "Success",
            $"Admin unlocked user account {user.Email}",
            ipAddress);

        TempData["SuccessMessage"] = $"User '{user.Email}' has been successfully unlocked.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        return View(new AdminResetPasswordViewModel
        {
            UserId = user.Id,
            UserName = user.Email ?? user.UserName ?? string.Empty
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(AdminResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user == null)
        {
            return NotFound();
        }

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, resetToken, model.NewPassword);

        if (result.Succeeded)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _auditService.LogAsync(
                GetCurrentUserId(),
                GetCurrentUserName(),
                "Security",
                "User",
                user.Id,
                null,
                null,
                "Success",
                $"Admin reset password for user {user.Email}",
                ipAddress);

            TempData["SuccessMessage"] = $"Password for '{user.Email}' has been reset successfully.";
            return RedirectToAction(nameof(Index));
        }

        foreach (var err in result.Errors)
        {
            ModelState.AddModelError(string.Empty, err.Description);
        }

        return View(model);
    }
}
