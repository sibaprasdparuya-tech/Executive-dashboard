using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AcxiomCRM.Tests;

public class ValidationAndBusinessRulesTests
{
    private ApplicationDbContext GetInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Opportunity_Amount_ZeroOrNegative_Is_Rejected()
    {
        // Section 5.3 & 17.7 & 17.19 #5: Create an Opportunity with Amount <= 0 -> rejected.
        var context = GetInMemoryDbContext("OppAmountDb");
        var audit = new AuditService(context);
        var oppService = new OpportunityService(context, audit);

        var oppZero = new Opportunity
        {
            OpportunityName = "Zero Deal",
            Amount = 0m,
            Probability = 50,
            ExpectedCloseDate = DateTime.Today.AddDays(10),
            Status = OpportunityStatuses.Open
        };

        var result = await oppService.CreateAsync(oppZero, "user1", "User One", "127.0.0.1");

        Assert.False(result.Success);
        Assert.Equal("Opportunity Amount must be greater than 0.", result.ErrorMessage);

        // Model validation attribute test
        var validationContext = new ValidationContext(oppZero);
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(oppZero, validationContext, validationResults, true);

        Assert.Contains(validationResults, r => r.ErrorMessage == "Opportunity Amount must be greater than 0.");
    }

    [Fact]
    public async Task Opportunity_Probability_GreaterThan100_Is_Rejected()
    {
        // Section 5.3 & 17.7 & 17.19 #6: Create an Opportunity with Probability > 100 -> rejected.
        var context = GetInMemoryDbContext("OppProbDb");
        var audit = new AuditService(context);
        var oppService = new OpportunityService(context, audit);

        var oppOver = new Opportunity
        {
            OpportunityName = "Over Probability Deal",
            Amount = 1000m,
            Probability = 101,
            ExpectedCloseDate = DateTime.Today.AddDays(10),
            Status = OpportunityStatuses.Open
        };

        var result = await oppService.CreateAsync(oppOver, "user1", "User One", "127.0.0.1");

        Assert.False(result.Success);
        Assert.Equal("Probability must be between 0 and 100.", result.ErrorMessage);
    }

    [Fact]
    public async Task Opportunity_ExpectedCloseDate_InPast_Is_Rejected()
    {
        // Section 5.3 & 17.7 & 17.19 #7: Set Expected Close Date in the past -> rejected for an active opportunity.
        var context = GetInMemoryDbContext("OppDateDb");
        var audit = new AuditService(context);
        var oppService = new OpportunityService(context, audit);

        var oppPast = new Opportunity
        {
            OpportunityName = "Past Close Date Deal",
            Amount = 5000m,
            Probability = 50,
            ExpectedCloseDate = DateTime.Today.AddDays(-1),
            Status = OpportunityStatuses.Open
        };

        var result = await oppService.CreateAsync(oppPast, "user1", "User One", "127.0.0.1");

        Assert.False(result.Success);
        Assert.Equal("Expected Close Date cannot be in the past.", result.ErrorMessage);
    }

    [Fact]
    public void Opportunity_WeightedPipeline_Is_CorrectlyCalculated()
    {
        // Section 4.8 & 17.11: Amount * Probability / 100
        var opp = new Opportunity
        {
            OpportunityName = "Enterprise Plan",
            Amount = 50000m,
            Probability = 40,
            ExpectedCloseDate = DateTime.Today.AddDays(30),
            Status = OpportunityStatuses.Open
        };

        Assert.Equal(20000m, opp.WeightedPipeline);
    }

    [Fact]
    public async Task FollowUp_Date_InPast_Is_Rejected_For_NewOrPlanned()
    {
        // Section 5.3 & 17.7 & 17.19 #8: Create a Follow-Up dated before today -> rejected for a new/planned follow-up.
        var context = GetInMemoryDbContext("FollowUpPastDb");
        var audit = new AuditService(context);
        var followService = new FollowUpService(context, audit);

        var followPast = new FollowUp
        {
            Subject = "Old Meeting",
            FollowUpDate = DateTime.Today.AddDays(-2),
            Status = FollowUpStatuses.Planned,
            FollowUpType = FollowUpTypes.Meeting
        };

        var result = await followService.CreateAsync(followPast, "user1", "User One", "127.0.0.1");

        Assert.False(result.Success);
        Assert.Equal("Follow-up date cannot be earlier than today.", result.ErrorMessage);
    }

    [Fact]
    public async Task Customer_Duplicate_Email_Or_Phone_Is_Rejected()
    {
        // Section 17.5 & 17.7 & 17.19 #3, #4: Duplicate customer creation must be prevented.
        var context = GetInMemoryDbContext("CustomerDupDb");
        var audit = new AuditService(context);
        var custService = new CustomerService(context, audit);

        var cust1 = new Customer
        {
            CustomerName = "First Company",
            Email = "duplicate@company.com",
            Phone = "+1-555-9999",
            Status = CustomerStatuses.Active
        };

        var res1 = await custService.CreateAsync(cust1, "user1", "User One", "127.0.0.1");
        Assert.True(res1.Success);

        // Attempt duplicate email
        var cust2 = new Customer
        {
            CustomerName = "Second Company",
            Email = "duplicate@company.com",
            Phone = "+1-555-8888",
            Status = CustomerStatuses.Active
        };

        var res2 = await custService.CreateAsync(cust2, "user1", "User One", "127.0.0.1");
        Assert.False(res2.Success);
        Assert.Equal("Email is already registered for another customer.", res2.ErrorMessage);

        // Attempt duplicate phone
        var cust3 = new Customer
        {
            CustomerName = "Third Company",
            Email = "third@company.com",
            Phone = "+1-555-9999",
            Status = CustomerStatuses.Active
        };

        var res3 = await custService.CreateAsync(cust3, "user1", "User One", "127.0.0.1");
        Assert.False(res3.Success);
        Assert.Equal("Phone number is already registered for another customer.", res3.ErrorMessage);
    }
}
