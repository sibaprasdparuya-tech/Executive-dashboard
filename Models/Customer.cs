using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public class Customer
{
    [Key]
    public int CustomerId { get; set; }

    [Display(Name = "Customer Code")]
    [StringLength(20)]
    public string CustomerCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Customer Name is required.")]
    [StringLength(100, ErrorMessage = "Customer Name cannot exceed 100 characters.")]
    [Display(Name = "Customer Name")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required.")]
    [RegularExpression(@"^[0-9\+\-\s\(\)]{7,20}$", ErrorMessage = "Enter a valid phone number.")]
    [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters.")]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "Company Name")]
    [StringLength(150, ErrorMessage = "Company Name cannot exceed 150 characters.")]
    public string? CompanyName { get; set; }

    [StringLength(250, ErrorMessage = "Address cannot exceed 250 characters.")]
    public string? Address { get; set; }

    [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
    public string? City { get; set; }

    [StringLength(100, ErrorMessage = "State cannot exceed 100 characters.")]
    public string? State { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = CustomerStatuses.Active;

    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Display(Name = "Modified Date")]
    public DateTime? ModifiedDate { get; set; }

    [Display(Name = "Assigned To / Owner")]
    public string? CreatedBy { get; set; }

    [ForeignKey("CreatedBy")]
    public virtual ApplicationUser? Owner { get; set; }

    // Navigation properties
    public virtual ICollection<Lead> Leads { get; set; } = new List<Lead>();
    public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
    public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
}
