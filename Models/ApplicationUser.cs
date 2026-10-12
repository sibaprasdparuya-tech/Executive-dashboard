using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
    public virtual ICollection<Lead> Leads { get; set; } = new List<Lead>();
    public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
    public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
}
