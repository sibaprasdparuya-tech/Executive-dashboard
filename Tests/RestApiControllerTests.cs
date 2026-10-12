using System.Security.Claims;
using AcxiomCRM.Controllers.Api;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AcxiomCRM.Tests;

public class RestApiControllerTests
{
    private ApplicationDbContext GetInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    private ControllerContext CreateControllerContext(string userId, string role, string email)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, email),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }

    [Fact]
    public async Task Api_Opportunities_Amount_ZeroOrNegative_Returns_BadRequest()
    {
        // Section 10 & 17.14 & 17.19 #5: Create an Opportunity with Amount <= 0 -> rejected
        var context = GetInMemoryDbContext("ApiOppAmountDb");
        var audit = new AuditService(context);
        var oppService = new OpportunityService(context, audit);

        var controller = new OpportunitiesApiController(oppService, null!)
        {
            ControllerContext = CreateControllerContext("user_1", UserRoles.SalesExecutive, "sales@acxiomcrm.com")
        };

        var dto = new OpportunityDto
        {
            OpportunityName = "Bad Amount Deal",
            Amount = 0m,
            Probability = 50,
            ExpectedCloseDate = DateTime.Today.AddDays(30)
        };

        var actionResult = await controller.CreateOpportunity(dto);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Api_Opportunities_Probability_Over100_Returns_BadRequest()
    {
        // Section 10 & 17.14 & 17.19 #6: Create an Opportunity with Probability > 100 -> rejected
        var context = GetInMemoryDbContext("ApiOppProbDb");
        var audit = new AuditService(context);
        var oppService = new OpportunityService(context, audit);

        var controller = new OpportunitiesApiController(oppService, null!)
        {
            ControllerContext = CreateControllerContext("user_1", UserRoles.SalesExecutive, "sales@acxiomcrm.com")
        };

        var dto = new OpportunityDto
        {
            OpportunityName = "Bad Prob Deal",
            Amount = 50000m,
            Probability = 105,
            ExpectedCloseDate = DateTime.Today.AddDays(30)
        };

        var actionResult = await controller.CreateOpportunity(dto);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Api_Opportunities_CloseDate_InPast_Returns_BadRequest()
    {
        // Section 10 & 17.14 & 17.19 #7: Set Expected Close Date in the past -> rejected
        var context = GetInMemoryDbContext("ApiOppCloseDateDb");
        var audit = new AuditService(context);
        var oppService = new OpportunityService(context, audit);

        var controller = new OpportunitiesApiController(oppService, null!)
        {
            ControllerContext = CreateControllerContext("user_1", UserRoles.SalesExecutive, "sales@acxiomcrm.com")
        };

        var dto = new OpportunityDto
        {
            OpportunityName = "Past Close Deal",
            Amount = 25000m,
            Probability = 50,
            ExpectedCloseDate = DateTime.Today.AddDays(-5)
        };

        var actionResult = await controller.CreateOpportunity(dto);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Api_FollowUps_Date_InPast_Returns_BadRequest()
    {
        // Section 10 & 17.14 & 17.19 #8: Create a Follow-Up dated before today -> rejected
        var context = GetInMemoryDbContext("ApiFollowUpDateDb");
        var audit = new AuditService(context);
        var followService = new FollowUpService(context, audit);

        var controller = new FollowUpsApiController(followService, null!)
        {
            ControllerContext = CreateControllerContext("user_1", UserRoles.SalesExecutive, "sales@acxiomcrm.com")
        };

        var dto = new FollowUpDto
        {
            Subject = "Past date meeting",
            FollowUpDate = DateTime.Today.AddDays(-3),
            Status = FollowUpStatuses.Planned
        };

        var actionResult = await controller.CreateFollowUp(dto);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Api_Customers_CRUD_And_Duplicate_Conflict()
    {
        // Section 10 & 17.14 & 17.19 #13: Call /api/customers -> authorized JSON response is returned
        var context = GetInMemoryDbContext("ApiCustCrudDb");
        var audit = new AuditService(context);
        var custService = new CustomerService(context, audit);

        var controller = new CustomersApiController(custService, null!)
        {
            ControllerContext = CreateControllerContext("user_1", UserRoles.Admin, "admin@acxiomcrm.com")
        };

        var newCust = new CustomerDto
        {
            CustomerName = "Wayne Industries",
            Email = "bruce@wayne.com",
            Phone = "+1-555-1234",
            CompanyName = "Wayne Enterprises"
        };

        var createResult = await controller.CreateCustomer(newCust);
        var createdAction = Assert.IsType<CreatedAtActionResult>(createResult.Result);
        Assert.Equal(201, createdAction.StatusCode);

        // Fetch customers list
        var listResult = await controller.GetCustomers(null, null);
        var okResult = Assert.IsType<OkObjectResult>(listResult.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<CustomerDto>>(okResult.Value);
        Assert.Single(list);

        // Attempt duplicate creation -> 409 Conflict
        var dupCust = new CustomerDto
        {
            CustomerName = "Another Wayne",
            Email = "bruce@wayne.com",
            Phone = "+1-555-9999"
        };

        var dupResult = await controller.CreateCustomer(dupCust);
        var conflictResult = Assert.IsType<ConflictObjectResult>(dupResult.Result);
        Assert.Equal(409, conflictResult.StatusCode);
    }

    [Fact]
    public void JwtTokenService_Generates_Valid_Bearer_Token()
    {
        // Section 10.1: REST API Token Authentication
        var inMemoryConfig = new Dictionary<string, string?>
        {
            {"JwtSettings:Secret", "SuperSecretLongKeyForJwtTokensTesting2026_AtLeast32Chars!"},
            {"JwtSettings:Issuer", "AcxiomCRM"},
            {"JwtSettings:Audience", "AcxiomCRMClient"},
            {"JwtSettings:ExpiryInMinutes", "60"}
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();
        var jwtService = new JwtTokenService(config);

        var user = new ApplicationUser
        {
            Id = "user_456",
            UserName = "test@company.com",
            Email = "test@company.com",
            FullName = "Test User"
        };

        var token = jwtService.GenerateToken(user, UserRoles.Manager);

        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.Contains(".", token); // Standard JWT header.payload.signature structure
    }
}
