using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AcxiomCRM.Tests;

public class WorkflowAndRoleScopingTests
{
    private ApplicationDbContext GetInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Lead_Conversion_Workflow_Creates_Customer_Opportunity_And_Audits()
    {
        // Section 8: Lead-to-Customer conversion workflow
        var context = GetInMemoryDbContext("LeadConvertDb");
        var audit = new AuditService(context);
        var custService = new CustomerService(context, audit);
        var leadService = new LeadService(context, audit, custService);

        var lead = new Lead
        {
            LeadName = "Stark Industries",
            Email = "tony@stark.com",
            Phone = "+1-555-3000",
            CompanyName = "Stark Industries",
            Source = LeadSources.Website,
            Status = LeadStatuses.Qualified,
            ExpectedValue = 250000m,
            AssignedTo = "sales_user_1"
        };

        var leadRes = await leadService.CreateAsync(lead, "sales_user_1", "Sales Rep", "127.0.0.1");
        Assert.True(leadRes.Success);

        // Convert lead
        var convertDto = new ConvertLeadDto
        {
            LeadId = lead.LeadId,
            CreateOpportunity = true,
            OpportunityName = "Arc Reactor Contract",
            OpportunityAmount = 300000m,
            ExpectedCloseDate = DateTime.Today.AddDays(45),
            Notes = "Qualified deal"
        };

        var convRes = await leadService.ConvertLeadAsync(convertDto, "sales_user_1", "Sales Rep", "127.0.0.1");

        Assert.True(convRes.Success);
        Assert.NotNull(convRes.CustomerId);
        Assert.NotNull(convRes.OpportunityId);

        // Verify Customer was created
        var customer = await context.Customers.FindAsync(convRes.CustomerId);
        Assert.NotNull(customer);
        Assert.Equal("tony@stark.com", customer.Email);
        Assert.Equal("Stark Industries", customer.CustomerName);

        // Verify Opportunity was created
        var opp = await context.Opportunities.FindAsync(convRes.OpportunityId);
        Assert.NotNull(opp);
        Assert.Equal("Arc Reactor Contract", opp.OpportunityName);
        Assert.Equal(300000m, opp.Amount);

        // Verify Lead status changed to Converted
        var updatedLead = await context.Leads.FindAsync(lead.LeadId);
        Assert.Equal(LeadStatuses.Converted, updatedLead?.Status);

        // Verify Audit Log entry created (Section 17.19 #12)
        var auditLogs = await audit.GetLogsAsync(entityName: "Lead", action: "Convert");
        Assert.NotEmpty(auditLogs);
        Assert.Equal("Lead", auditLogs[0].EntityName);
        Assert.Equal("Convert", auditLogs[0].Action);
    }

    [Fact]
    public async Task RoleScoping_SalesExecutive_Only_Sees_Assigned_Customers()
    {
        // Section 7.1 & 17.10 & 17.19 #9: Login as SalesExecutive -> only authorized/assigned sales scope is accessible.
        var context = GetInMemoryDbContext("RoleScopeDb");
        var audit = new AuditService(context);
        var custService = new CustomerService(context, audit);

        var custSales1 = new Customer
        {
            CustomerName = "Sales 1 Client",
            Email = "client1@sales1.com",
            Phone = "111-111-1111",
            CreatedBy = "sales_user_1",
            Status = CustomerStatuses.Active
        };

        var custSales2 = new Customer
        {
            CustomerName = "Sales 2 Client",
            Email = "client2@sales2.com",
            Phone = "222-222-2222",
            CreatedBy = "sales_user_2",
            Status = CustomerStatuses.Active
        };

        await custService.CreateAsync(custSales1, "sales_user_1", "Sales 1", "127.0.0.1");
        await custService.CreateAsync(custSales2, "sales_user_2", "Sales 2", "127.0.0.1");

        // Sales 1 accesses customers
        var sales1View = await custService.GetAllAsync("sales_user_1", UserRoles.SalesExecutive);
        Assert.Single(sales1View);
        Assert.Equal("Sales 1 Client", sales1View[0].CustomerName);

        // Manager accesses customers (Section 17.19 #10: Login as Manager -> team pipeline/report access is available)
        var managerView = await custService.GetAllAsync("manager_user", UserRoles.Manager);
        Assert.Equal(2, managerView.Count);

        // Admin accesses customers (Section 17.19 #11: Login as Admin -> full administration)
        var adminView = await custService.GetAllAsync("admin_user", UserRoles.Admin);
        Assert.Equal(2, adminView.Count);
    }

    [Fact]
    public async Task AuditLogging_Records_Operations_With_Complete_Metadata()
    {
        // Section 17.16: Audit records capture UserId, Action, EntityName, RecordId, OldValue, NewValue, CreatedDate, IpAddress
        var context = GetInMemoryDbContext("AuditTrailDb");
        var audit = new AuditService(context);

        await audit.LogAsync(
            "user_123",
            "testuser@company.com",
            "Update",
            "Opportunity",
            "99",
            "Amount: $10,000",
            "Amount: $20,000",
            "Success",
            "Updated deal value after client renegotiation",
            "192.168.1.100"
        );

        var logs = await audit.GetLogsAsync(userId: "user_123");
        Assert.Single(logs);
        var entry = logs[0];
        Assert.Equal("user_123", entry.UserId);
        Assert.Equal("testuser@company.com", entry.UserName);
        Assert.Equal("Update", entry.Action);
        Assert.Equal("Opportunity", entry.EntityName);
        Assert.Equal("99", entry.RecordId);
        Assert.Equal("Amount: $10,000", entry.OldValue);
        Assert.Equal("Amount: $20,000", entry.NewValue);
        Assert.Equal("Success", entry.Result);
        Assert.Equal("192.168.1.100", entry.IpAddress);
    }
}
