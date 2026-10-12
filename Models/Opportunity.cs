using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public class Opportunity : IValidatableObject
{
    [Key]
    public int OpportunityId { get; set; }

    [Required(ErrorMessage = "Opportunity Name is required.")]
    [StringLength(150, ErrorMessage = "Opportunity Name cannot exceed 150 characters.")]
    [Display(Name = "Opportunity Name")]
    public string OpportunityName { get; set; } = string.Empty;

    [Display(Name = "Customer")]
    public int? CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    [Display(Name = "Lead")]
    public int? LeadId { get; set; }

    [ForeignKey("LeadId")]
    public virtual Lead? Lead { get; set; }

    [Required(ErrorMessage = "Opportunity Amount is required.")]
    [Range(0.01, 1000000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Amount ($)")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Stage is required.")]
    [StringLength(50)]
    public string Stage { get; set; } = OpportunityStages.Qualification;

    [Required(ErrorMessage = "Probability is required.")]
    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    [Display(Name = "Probability (%)")]
    public int Probability { get; set; } = 20;

    [Required(ErrorMessage = "Expected Close Date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Expected Close Date")]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddDays(30);

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = OpportunityStatuses.Open;

    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }

    [ForeignKey("AssignedTo")]
    public virtual ApplicationUser? AssignedUser { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    [NotMapped]
    [Display(Name = "Weighted Pipeline")]
    public decimal WeightedPipeline => Math.Round(Amount * Probability / 100m, 2);

    // Server-side business validations (Section 5.3 & 17.7)
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Amount <= 0)
        {
            yield return new ValidationResult(
                "Opportunity Amount must be greater than 0.",
                new[] { nameof(Amount) });
        }

        if (Probability < 0 || Probability > 100)
        {
            yield return new ValidationResult(
                "Probability must be between 0 and 100.",
                new[] { nameof(Probability) });
        }

        // Expected Close Date cannot be in the past for an active / open opportunity
        if (Status == OpportunityStatuses.Open && ExpectedCloseDate.Date < DateTime.Today)
        {
            yield return new ValidationResult(
                "Expected Close Date cannot be in the past.",
                new[] { nameof(ExpectedCloseDate) });
        }
    }

    // Navigation properties
    public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
}
