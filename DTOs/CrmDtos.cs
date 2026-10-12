using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.DTOs;

public class LoginRequestDto
{
    [Required(ErrorMessage = "Email / Username is required.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string? Role { get; set; }
    public DateTime? Expiration { get; set; }
}

public class CustomerDto
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Customer Name is required.")]
    [StringLength(100, ErrorMessage = "Customer Name cannot exceed 100 characters.")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required.")]
    [RegularExpression(@"^[0-9\+\-\s\(\)]{7,20}$", ErrorMessage = "Enter a valid phone number.")]
    public string Phone { get; set; } = string.Empty;

    public string? CompanyName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? OwnerName { get; set; }
}

public class LeadDto
{
    public int LeadId { get; set; }
    public string LeadCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lead Name is required.")]
    [StringLength(100, ErrorMessage = "Lead Name cannot exceed 100 characters.")]
    public string LeadName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required.")]
    [RegularExpression(@"^[0-9\+\-\s\(\)]{7,20}$", ErrorMessage = "Enter a valid phone number.")]
    public string Phone { get; set; } = string.Empty;

    public string? CompanyName { get; set; }
    public string Source { get; set; } = "Website";
    public string Status { get; set; } = "New";
    public string Priority { get; set; } = "Medium";

    [Range(0, 100000000, ErrorMessage = "Expected Value must be greater than or equal to 0.")]
    public decimal ExpectedValue { get; set; }

    public DateTime CreatedDate { get; set; }
    public string? AssignedTo { get; set; }
    public string? AssignedUserName { get; set; }
    public int? ConvertedCustomerId { get; set; }
    public int? ConvertedOpportunityId { get; set; }
    public string? Notes { get; set; }
}

public class ConvertLeadDto
{
    public int LeadId { get; set; }
    public bool CreateOpportunity { get; set; } = true;
    public string? OpportunityName { get; set; }
    public decimal OpportunityAmount { get; set; }
    public DateTime? ExpectedCloseDate { get; set; }
    public string? Notes { get; set; }
}

public class OpportunityDto
{
    public int OpportunityId { get; set; }

    [Required(ErrorMessage = "Opportunity Name is required.")]
    [StringLength(150, ErrorMessage = "Opportunity Name cannot exceed 150 characters.")]
    public string OpportunityName { get; set; } = string.Empty;

    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? LeadId { get; set; }
    public string? LeadName { get; set; }

    [Required(ErrorMessage = "Opportunity Amount is required.")]
    [Range(0.01, 1000000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Stage is required.")]
    public string Stage { get; set; } = "Qualification";

    [Required(ErrorMessage = "Probability is required.")]
    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    public int Probability { get; set; } = 20;

    [Required(ErrorMessage = "Expected Close Date is required.")]
    public DateTime ExpectedCloseDate { get; set; }

    public string Status { get; set; } = "Open";
    public DateTime CreatedDate { get; set; }
    public string? AssignedTo { get; set; }
    public string? AssignedUserName { get; set; }
    public string? Notes { get; set; }
    public decimal WeightedPipeline { get; set; }
}

public class FollowUpDto
{
    public int FollowUpId { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? LeadId { get; set; }
    public string? LeadName { get; set; }
    public int? OpportunityId { get; set; }
    public string? OpportunityName { get; set; }
    public string? RelatedType { get; set; }
    public int? RelatedId { get; set; }

    [Required(ErrorMessage = "Follow-up Date is required.")]
    public DateTime FollowUpDate { get; set; }

    [Required(ErrorMessage = "Follow-up Type is required.")]
    public string FollowUpType { get; set; } = "Call";

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
    public string Subject { get; set; } = string.Empty;

    public string? Remarks { get; set; }
    public string Status { get; set; } = "Planned";
    public string? AssignedTo { get; set; }
    public string? AssignedUserName { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsOverdue { get; set; }
}

public class PipelineReportDto
{
    public decimal TotalPipelineValue { get; set; }
    public decimal TotalWeightedPipelineValue { get; set; }
    public int TotalOpportunitiesCount { get; set; }
    public List<StageSummaryDto> ByStage { get; set; } = new();
    public List<OwnerSummaryDto> ByOwner { get; set; } = new();
}

public class StageSummaryDto
{
    public string Stage { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal WeightedAmount { get; set; }
}

public class OwnerSummaryDto
{
    public string OwnerName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal WeightedAmount { get; set; }
}
