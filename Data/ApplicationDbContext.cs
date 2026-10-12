using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Models;

namespace AcxiomCRM.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Unique constraints on Customer Email and Phone
        builder.Entity<Customer>()
            .HasIndex(c => c.Email)
            .IsUnique();

        builder.Entity<Customer>()
            .HasIndex(c => c.Phone)
            .IsUnique();

        // Customer relationships
        builder.Entity<Customer>()
            .HasOne(c => c.Owner)
            .WithMany(u => u.Customers)
            .HasForeignKey(c => c.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);

        // Lead relationships
        builder.Entity<Lead>()
            .HasOne(l => l.AssignedUser)
            .WithMany(u => u.Leads)
            .HasForeignKey(l => l.AssignedTo)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Lead>()
            .HasOne(l => l.ConvertedCustomer)
            .WithMany()
            .HasForeignKey(l => l.ConvertedCustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Lead>()
            .HasOne(l => l.ConvertedOpportunity)
            .WithMany()
            .HasForeignKey(l => l.ConvertedOpportunityId)
            .OnDelete(DeleteBehavior.SetNull);

        // Opportunity relationships
        builder.Entity<Opportunity>()
            .HasOne(o => o.Customer)
            .WithMany(c => c.Opportunities)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Opportunity>()
            .HasOne(o => o.Lead)
            .WithMany(l => l.Opportunities)
            .HasForeignKey(o => o.LeadId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Opportunity>()
            .HasOne(o => o.AssignedUser)
            .WithMany(u => u.Opportunities)
            .HasForeignKey(o => o.AssignedTo)
            .OnDelete(DeleteBehavior.SetNull);

        // FollowUp relationships
        builder.Entity<FollowUp>()
            .HasOne(f => f.Customer)
            .WithMany(c => c.FollowUps)
            .HasForeignKey(f => f.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<FollowUp>()
            .HasOne(f => f.Lead)
            .WithMany(l => l.FollowUps)
            .HasForeignKey(f => f.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<FollowUp>()
            .HasOne(f => f.Opportunity)
            .WithMany(o => o.FollowUps)
            .HasForeignKey(f => f.OpportunityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<FollowUp>()
            .HasOne(f => f.AssignedUser)
            .WithMany(u => u.FollowUps)
            .HasForeignKey(f => f.AssignedTo)
            .OnDelete(DeleteBehavior.SetNull);

        // Activity relationships
        builder.Entity<Activity>()
            .HasOne(a => a.Customer)
            .WithMany(c => c.Activities)
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Activity>()
            .HasOne(a => a.Lead)
            .WithMany(l => l.Activities)
            .HasForeignKey(a => a.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Activity>()
            .HasOne(a => a.Opportunity)
            .WithMany(o => o.Activities)
            .HasForeignKey(a => a.OpportunityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Activity>()
            .HasOne(a => a.AssignedUser)
            .WithMany(u => u.Activities)
            .HasForeignKey(a => a.AssignedTo)
            .OnDelete(DeleteBehavior.SetNull);

        // AuditLog relationships
        builder.Entity<AuditLog>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
