using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public class AuditLog
{
    [Key]
    public int AuditLogId { get; set; }

    [Display(Name = "User ID")]
    public string? UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }

    [Display(Name = "User Name / Email")]
    [StringLength(150)]
    public string? UserName { get; set; }

    [Required]
    [StringLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "Module / Entity")]
    public string EntityName { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Record ID")]
    public string? RecordId { get; set; }

    [Display(Name = "Old Value")]
    public string? OldValue { get; set; }

    [Display(Name = "New Value")]
    public string? NewValue { get; set; }

    [StringLength(50)]
    public string Result { get; set; } = "Success";

    public string? Details { get; set; }

    [Display(Name = "Timestamp")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [StringLength(50)]
    [Display(Name = "IP Address")]
    public string? IpAddress { get; set; }
}
