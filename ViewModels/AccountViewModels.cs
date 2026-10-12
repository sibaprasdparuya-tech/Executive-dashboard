using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me?")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "The password must be at least 8 characters long and meet complexity requirements.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    public string Role { get; set; } = "SalesExecutive";
}

public class UserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int AccessFailedCount { get; set; }
    public bool IsLockedOut { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class UserEditViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}

public class AdminResetPasswordViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "New Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "The password and confirmation do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
