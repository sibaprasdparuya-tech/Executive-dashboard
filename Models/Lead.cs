using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public class Lead
{
    [Key]
    public int LeadId { get; set; }

    [Display(Name = "Lead Code")]
    [StringLength(20)]
    public string LeadCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lead Name is required.")]
    [StringLength(100, ErrorMessage = "Lead Name cannot exceed 100 characters.")]
    [Display(Name = "Lead Name")]
    public string LeadName { get; set; } = string.Empty;

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

    [Required(ErrorMessage = "Source is required.")]
    [StringLength(50)]
    public string Source { get; set; } = LeadSources.Website;

    [Required(ErrorMessage = "Lead status is required.")]
    [StringLength(50)]
    public string Status { get; set; } = LeadStatuses.New;

    [StringLength(50)]
    public string Priority { get; set; } = "Medium";

    [Range(0, 100000000, ErrorMessage = "Expected Value must be greater than or equal to 0.")]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Expected Value")]
    public decimal ExpectedValue { get; set; } = 0;

    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }

    [ForeignKey("AssignedTo")]
    public virtual ApplicationUser? AssignedUser { get; set; }

    [Display(Name = "Converted Customer")]
    public int? ConvertedCustomerId { get; set; }

    [ForeignKey("ConvertedCustomerId")]
    public virtual Customer? ConvertedCustomer { get; set; }

    [Display(Name = "Converted Opportunity")]
    public int? ConvertedOpportunityId { get; set; }

    [ForeignKey("ConvertedOpportunityId")]
    public virtual Opportunity? ConvertedOpportunity { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    // Navigation properties
    public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
    public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
}
