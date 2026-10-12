using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public class FollowUp : IValidatableObject
{
    [Key]
    public int FollowUpId { get; set; }

    [Display(Name = "Customer")]
    public int? CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    [Display(Name = "Lead")]
    public int? LeadId { get; set; }

    [ForeignKey("LeadId")]
    public virtual Lead? Lead { get; set; }

    [Display(Name = "Opportunity")]
    public int? OpportunityId { get; set; }

    [ForeignKey("OpportunityId")]
    public virtual Opportunity? Opportunity { get; set; }

    [StringLength(50)]
    [Display(Name = "Related Type")]
    public string? RelatedType { get; set; }

    [Display(Name = "Related ID")]
    public int? RelatedId { get; set; }

    [Required(ErrorMessage = "Follow-up Date is required.")]
    [Display(Name = "Follow-up Date")]
    public DateTime FollowUpDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Follow-up Type is required.")]
    [StringLength(50)]
    [Display(Name = "Type")]
    public string FollowUpType { get; set; } = FollowUpTypes.Call;

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
    public string Subject { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Remarks { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    [StringLength(50)]
    public string Status { get; set; } = FollowUpStatuses.Planned;

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }

    [ForeignKey("AssignedTo")]
    public virtual ApplicationUser? AssignedUser { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public bool IsOverdue => Status == FollowUpStatuses.Planned && FollowUpDate.Date < DateTime.Today;

    // Server-side business validations (Section 5.3 & 17.7 & 17.19)
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Follow-up date cannot be earlier than today for a new/planned follow-up
        if (Status == FollowUpStatuses.Planned && FollowUpDate.Date < DateTime.Today)
        {
            yield return new ValidationResult(
                "Follow-up date cannot be earlier than today.",
                new[] { nameof(FollowUpDate) });
        }
    }
}
