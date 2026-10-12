using System.Security.Claims;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

[Authorize]
public class CustomersController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomersController(ICustomerService customerService, UserManager<ApplicationUser> userManager)
    {
        _customerService = customerService;
        _userManager = userManager;
    }

    private string GetCurrentUserId() => _userManager.GetUserId(User) ?? string.Empty;
    private string GetCurrentUserName() => User.Identity?.Name ?? "User";
    private string GetCurrentRole() => User.IsInRole(UserRoles.Admin)
        ? UserRoles.Admin
        : (User.IsInRole(UserRoles.Manager) ? UserRoles.Manager : UserRoles.SalesExecutive);

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, string? status, int page = 1)
    {
        var customers = await _customerService.GetAllAsync(GetCurrentUserId(), GetCurrentRole(), searchTerm, status);
        ViewData["SearchTerm"] = searchTerm;
        ViewData["Status"] = status;

        const int pageSize = 10;
        var totalCount = customers.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

        var pagedItems = customers.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        ViewData["CurrentPage"] = page;
        ViewData["TotalPages"] = totalPages;
        ViewData["TotalCount"] = totalCount;

        return View(pagedItems);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var customer = await _customerService.GetByIdAsync(id, GetCurrentUserId(), GetCurrentRole());
        if (customer == null)
        {
            return NotFound();
        }
        return View(customer);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var users = _userManager.Users.Where(u => u.IsActive).ToList();
        ViewBag.Users = users;
        return View(new Customer { CreatedBy = GetCurrentUserId() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Customer customer)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        // Server-side validation
        if (!ModelState.IsValid)
        {
            ViewBag.Users = _userManager.Users.Where(u => u.IsActive).ToList();
            return View(customer);
        }

        var result = await _customerService.CreateAsync(customer, GetCurrentUserId(), GetCurrentUserName(), ipAddress);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to create customer.");
            ViewBag.Users = _userManager.Users.Where(u => u.IsActive).ToList();
            return View(customer);
        }

        TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' created successfully with code {customer.CustomerCode}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var customer = await _customerService.GetByIdAsync(id, GetCurrentUserId(), GetCurrentRole());
        if (customer == null)
        {
            return NotFound();
        }

        ViewBag.Users = _userManager.Users.Where(u => u.IsActive).ToList();
        return View(customer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Customer customer)
    {
        if (id != customer.CustomerId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Users = _userManager.Users.Where(u => u.IsActive).ToList();
            return View(customer);
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _customerService.UpdateAsync(customer, GetCurrentUserId(), GetCurrentUserName(), ipAddress);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to update customer.");
            ViewBag.Users = _userManager.Users.Where(u => u.IsActive).ToList();
            return View(customer);
        }

        TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _customerService.DeleteAsync(id, GetCurrentUserId(), GetCurrentUserName(), ipAddress);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to delete customer.";
        }
        else
        {
            TempData["SuccessMessage"] = "Customer deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> CheckEmailAvailability(string email, int? customerId)
    {
        var isUnique = await _customerService.IsEmailUniqueAsync(email, customerId);
        return Json(isUnique);
    }

    [HttpGet]
    public async Task<IActionResult> CheckPhoneAvailability(string phone, int? customerId)
    {
        var isUnique = await _customerService.IsPhoneUniqueAsync(phone, customerId);
        return Json(isUnique);
    }
}
