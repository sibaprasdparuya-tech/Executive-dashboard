using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure database schema is created
        await context.Database.EnsureCreatedAsync();

        // 1. Seed Roles
        string[] roles = [UserRoles.Admin, UserRoles.Manager, UserRoles.SalesExecutive];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. Seed Users
        var adminUser = await userManager.FindByEmailAsync("admin@acxiomcrm.com");
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin@acxiomcrm.com",
                Email = "admin@acxiomcrm.com",
                FullName = "System Administrator",
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(adminUser, "Admin@12345");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, UserRoles.Admin);
            }
        }

        var managerUser = await userManager.FindByEmailAsync("manager@acxiomcrm.com");
        if (managerUser == null)
        {
            managerUser = new ApplicationUser
            {
                UserName = "manager@acxiomcrm.com",
                Email = "manager@acxiomcrm.com",
                FullName = "Sales Manager",
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(managerUser, "Manager@12345");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(managerUser, UserRoles.Manager);
            }
        }

        var salesUser = await userManager.FindByEmailAsync("sales@acxiomcrm.com");
        if (salesUser == null)
        {
            salesUser = new ApplicationUser
            {
                UserName = "sales@acxiomcrm.com",
                Email = "sales@acxiomcrm.com",
                FullName = "Sarah Sales Executive",
                EmailConfirmed = true,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(salesUser, "Sales@12345");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(salesUser, UserRoles.SalesExecutive);
            }
        }

        // 3. Seed Customers if empty
        if (!await context.Customers.AnyAsync())
        {
            var customers = new List<Customer>
            {
                new()
                {
                    CustomerCode = "CUST-0001",
                    CustomerName = "Apex Global Technologies",
                    Email = "contact@apextech.com",
                    Phone = "+1-555-0101",
                    CompanyName = "Apex Technologies Inc.",
                    Address = "100 Innovation Way",
                    City = "San Jose",
                    State = "CA",
                    Status = CustomerStatuses.Active,
                    CreatedBy = salesUser.Id,
                    CreatedDate = DateTime.UtcNow.AddMonths(-3)
                },
                new()
                {
                    CustomerCode = "CUST-0002",
                    CustomerName = "BlueHorizon Logistics",
                    Email = "info@bluehorizon.io",
                    Phone = "+1-555-0102",
                    CompanyName = "BlueHorizon Global",
                    Address = "45 Harbor Drive",
                    City = "Seattle",
                    State = "WA",
                    Status = CustomerStatuses.Active,
                    CreatedBy = salesUser.Id,
                    CreatedDate = DateTime.UtcNow.AddMonths(-2)
                },
                new()
                {
                    CustomerCode = "CUST-0003",
                    CustomerName = "Crestview Financial Partners",
                    Email = "partners@crestviewfin.com",
                    Phone = "+1-555-0103",
                    CompanyName = "Crestview Holdings",
                    Address = "88 Wall Street",
                    City = "New York",
                    State = "NY",
                    Status = CustomerStatuses.Active,
                    CreatedBy = managerUser.Id,
                    CreatedDate = DateTime.UtcNow.AddMonths(-1)
                },
                new()
                {
                    CustomerCode = "CUST-0004",
                    CustomerName = "Delta Pharma Solutions",
                    Email = "procure@deltapharma.com",
                    Phone = "+1-555-0104",
                    CompanyName = "Delta Pharma Labs",
                    Address = "12 Science Park",
                    City = "Boston",
                    State = "MA",
                    Status = CustomerStatuses.Active,
                    CreatedBy = salesUser.Id,
                    CreatedDate = DateTime.UtcNow.AddDays(-15)
                }
            };
            await context.Customers.AddRangeAsync(customers);
            await context.SaveChangesAsync();
        }

        var cust1 = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-0001");
        var cust2 = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-0002");
        var cust3 = await context.Customers.FirstOrDefaultAsync(c => c.CustomerCode == "CUST-0003");

        // 4. Seed Leads if empty
        if (!await context.Leads.AnyAsync())
        {
            var leads = new List<Lead>
            {
                new()
                {
                    LeadCode = "LEAD-0001",
                    LeadName = "Zenith Retail Corp",
                    Email = "purchasing@zenithretail.com",
                    Phone = "+1-555-0201",
                    CompanyName = "Zenith Retail Corp",
                    Source = LeadSources.Website,
                    Status = LeadStatuses.Qualified,
                    Priority = "High",
                    ExpectedValue = 75000,
                    AssignedTo = salesUser.Id,
                    CreatedDate = DateTime.UtcNow.AddDays(-20),
                    Notes = "High interest in enterprise CRM package"
                },
                new()
                {
                    LeadCode = "LEAD-0002",
                    LeadName = "Vertex Health Systems",
                    Email = "it@vertexhealth.org",
                    Phone = "+1-555-0202",
                    CompanyName = "Vertex Health Systems",
                    Source = LeadSources.Referral,
                    Status = LeadStatuses.Contacted,
                    Priority = "Medium",
                    ExpectedValue = 45000,
                    AssignedTo = salesUser.Id,
                    CreatedDate = DateTime.UtcNow.AddDays(-10),
                    Notes = "Initial demo call completed"
                },
                new()
                {
                    LeadCode = "LEAD-0003",
                    LeadName = "Summit Media Group",
                    Email = "digital@summitmedia.com",
                    Phone = "+1-555-0203",
                    CompanyName = "Summit Media Group",
                    Source = LeadSources.ColdCall,
                    Status = LeadStatuses.New,
                    Priority = "Low",
                    ExpectedValue = 20000,
                    AssignedTo = managerUser.Id,
                    CreatedDate = DateTime.UtcNow.AddDays(-5),
                    Notes = "Requested brochure and pricing"
                },
                new()
                {
                    LeadCode = "LEAD-0004",
                    LeadName = "Nova Clean Energy",
                    Email = "procure@novaclean.com",
                    Phone = "+1-555-0204",
                    CompanyName = "Nova Clean Energy",
                    Source = LeadSources.Advertisement,
                    Status = LeadStatuses.Converted,
                    Priority = "High",
                    ExpectedValue = 120000,
                    AssignedTo = salesUser.Id,
                    ConvertedCustomerId = cust1?.CustomerId,
                    CreatedDate = DateTime.UtcNow.AddMonths(-1),
                    Notes = "Converted to Apex contract expansion"
                },
                new()
                {
                    LeadCode = "LEAD-0005",
                    LeadName = "Legacy Iron Works",
                    Email = "supply@legacyiron.com",
                    Phone = "+1-555-0205",
                    CompanyName = "Legacy Iron Works",
                    Source = LeadSources.Other,
                    Status = LeadStatuses.Lost,
                    Priority = "Low",
                    ExpectedValue = 15000,
                    AssignedTo = salesUser.Id,
                    CreatedDate = DateTime.UtcNow.AddDays(-40),
                    Notes = "Chose competitor solution"
                }
            };
            await context.Leads.AddRangeAsync(leads);
            await context.SaveChangesAsync();
        }

        // 5. Seed Opportunities if empty
        if (!await context.Opportunities.AnyAsync() && cust1 != null && cust2 != null && cust3 != null)
        {
            var opps = new List<Opportunity>
            {
                new()
                {
                    OpportunityName = "Apex Cloud Migration Contract",
                    CustomerId = cust1.CustomerId,
                    Amount = 85000,
                    Stage = OpportunityStages.Negotiation,
                    Probability = 80,
                    ExpectedCloseDate = DateTime.Today.AddDays(20),
                    Status = OpportunityStatuses.Open,
                    AssignedTo = salesUser.Id,
                    Notes = "Final contract terms under legal review",
                    CreatedDate = DateTime.UtcNow.AddDays(-25)
                },
                new()
                {
                    OpportunityName = "BlueHorizon Fleet Tracking Expansion",
                    CustomerId = cust2.CustomerId,
                    Amount = 145000,
                    Stage = OpportunityStages.Proposal,
                    Probability = 50,
                    ExpectedCloseDate = DateTime.Today.AddDays(45),
                    Status = OpportunityStatuses.Open,
                    AssignedTo = salesUser.Id,
                    Notes = "Proposal presented to CTO",
                    CreatedDate = DateTime.UtcNow.AddDays(-14)
                },
                new()
                {
                    OpportunityName = "Crestview Security Suite",
                    CustomerId = cust3.CustomerId,
                    Amount = 60000,
                    Stage = OpportunityStages.Won,
                    Probability = 100,
                    ExpectedCloseDate = DateTime.Today.AddDays(-5),
                    Status = OpportunityStatuses.Won,
                    AssignedTo = managerUser.Id,
                    Notes = "Deal signed and closed!",
                    CreatedDate = DateTime.UtcNow.AddDays(-30)
                },
                new()
                {
                    OpportunityName = "Apex Additional Seat Licenses",
                    CustomerId = cust1.CustomerId,
                    Amount = 28000,
                    Stage = OpportunityStages.Won,
                    Probability = 100,
                    ExpectedCloseDate = DateTime.Today.AddDays(-15),
                    Status = OpportunityStatuses.Won,
                    AssignedTo = salesUser.Id,
                    Notes = "Paid annual upfront",
                    CreatedDate = DateTime.UtcNow.AddMonths(-2)
                },
                new()
                {
                    OpportunityName = "BlueHorizon Data Analytics Add-on",
                    CustomerId = cust2.CustomerId,
                    Amount = 35000,
                    Stage = OpportunityStages.Qualification,
                    Probability = 30,
                    ExpectedCloseDate = DateTime.Today.AddDays(60),
                    Status = OpportunityStatuses.Open,
                    AssignedTo = salesUser.Id,
                    Notes = "Requirements gathering session scheduled",
                    CreatedDate = DateTime.UtcNow.AddDays(-7)
                }
            };
            await context.Opportunities.AddRangeAsync(opps);
            await context.SaveChangesAsync();
        }

        // 6. Seed Follow-ups if empty
        if (!await context.FollowUps.AnyAsync() && cust1 != null)
        {
            var opp1 = await context.Opportunities.FirstOrDefaultAsync(o => o.CustomerId == cust1.CustomerId);
            var lead1 = await context.Leads.FirstOrDefaultAsync(l => l.LeadCode == "LEAD-0001");

            var followUps = new List<FollowUp>
            {
                new()
                {
                    Subject = "Follow-up call on Apex Legal review",
                    CustomerId = cust1.CustomerId,
                    OpportunityId = opp1?.OpportunityId,
                    FollowUpType = FollowUpTypes.Call,
                    FollowUpDate = DateTime.Today.AddDays(2),
                    Status = FollowUpStatuses.Planned,
                    AssignedTo = salesUser.Id,
                    Remarks = "Confirm approval from their GC"
                },
                new()
                {
                    Subject = "Product Demo for Zenith Retail",
                    LeadId = lead1?.LeadId,
                    FollowUpType = FollowUpTypes.Demo,
                    FollowUpDate = DateTime.Today.AddDays(4),
                    Status = FollowUpStatuses.Planned,
                    AssignedTo = salesUser.Id,
                    Remarks = "Show automated workflow module"
                },
                new()
                {
                    Subject = "Send revision of BlueHorizon proposal",
                    CustomerId = cust2?.CustomerId,
                    FollowUpType = FollowUpTypes.Email,
                    FollowUpDate = DateTime.Today,
                    Status = FollowUpStatuses.Planned,
                    AssignedTo = salesUser.Id,
                    Remarks = "Discount terms requested"
                },
                new()
                {
                    Subject = "Overdue follow-up check-in",
                    CustomerId = cust3?.CustomerId,
                    FollowUpType = FollowUpTypes.Task,
                    FollowUpDate = DateTime.Today.AddDays(-2),
                    Status = FollowUpStatuses.Planned,
                    AssignedTo = managerUser.Id,
                    Remarks = "Annual review schedule check"
                },
                new()
                {
                    Subject = "Contract signing kickoff meeting",
                    CustomerId = cust3?.CustomerId,
                    FollowUpType = FollowUpTypes.Meeting,
                    FollowUpDate = DateTime.Today.AddDays(-10),
                    Status = FollowUpStatuses.Completed,
                    AssignedTo = managerUser.Id,
                    Remarks = "Completed successfully. All stakeholders attended."
                }
            };
            await context.FollowUps.AddRangeAsync(followUps);
            await context.SaveChangesAsync();
        }

        // 7. Seed Initial Audit Logs
        if (!await context.AuditLogs.AnyAsync())
        {
            var auditLogs = new List<AuditLog>
            {
                new()
                {
                    UserId = adminUser.Id,
                    UserName = adminUser.Email,
                    Action = "Security",
                    EntityName = "System",
                    RecordId = "0",
                    Result = "Success",
                    Details = "System initialized with baseline security policies and role configuration.",
                    CreatedDate = DateTime.UtcNow.AddMonths(-3),
                    IpAddress = "127.0.0.1"
                },
                new()
                {
                    UserId = adminUser.Id,
                    UserName = adminUser.Email,
                    Action = "Login",
                    EntityName = "Auth",
                    RecordId = adminUser.Id,
                    Result = "Success",
                    Details = "Administrator logged into AcxiomCRM system console.",
                    CreatedDate = DateTime.UtcNow.AddDays(-1),
                    IpAddress = "127.0.0.1"
                }
            };
            await context.AuditLogs.AddRangeAsync(auditLogs);
            await context.SaveChangesAsync();
        }
    }
}
