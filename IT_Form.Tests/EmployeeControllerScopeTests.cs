// Project comment: EmployeeControllerScopeTests.cs validates company-scoped employee roster and dropdown isolation.

using System.Security.Claims;
using IT_Form.Controllers;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IT_Form.Tests;

/// <summary>
/// Defines the EmployeeControllerScopeTests class used by the IT Form application.
/// </summary>
public sealed class EmployeeControllerScopeTests
{
    [Fact]
    /// <summary>
    /// Runs the EmployeeControllerIndex_ScopedUserSeesOnlyOwnCompanyRosterAndDropdowns operation.
    /// </summary>
    public async Task EmployeeControllerIndex_ScopedUserSeesOnlyOwnCompanyRosterAndDropdowns()
    {
        await using var context = CreateContext();
        context.OnboardingRequests.AddRange(
            new OnboardingRequest
            {
                Id = 1,
                FullName = "Alpha Employee",
                EmployeeID = "E-ALPHA",
                Department = "IT",
                Branch = "Alpha",
                Status = RequestStatus.Completed
            },
            new OnboardingRequest
            {
                Id = 2,
                FullName = "Beta Employee",
                EmployeeID = "E-BETA",
                Department = "Finance",
                Branch = "Beta",
                Status = RequestStatus.Completed
            });
        await context.SaveChangesAsync();

        var controller = new EmployeeController(context)
        {
            ControllerContext = CreateControllerContext(scopedToCompany: "Alpha")
        };

        var result = await controller.Index(searchString: "", department: "", branch: "", page: 1);

        var viewResult = Assert.IsType<ViewResult>(result);
        var roster = Assert.IsType<PaginatedList<OnboardingRequest>>(viewResult.Model);
        Assert.Single(roster);
        Assert.Equal("Alpha", roster[0].Branch);

        var branches = Assert.IsAssignableFrom<IEnumerable<string>>(viewResult.ViewData["Branches"]);
        Assert.DoesNotContain("Beta", branches);
        Assert.Contains("Alpha", branches);

        var departments = Assert.IsAssignableFrom<IEnumerable<string>>(viewResult.ViewData["Departments"]);
        Assert.Contains("IT", departments);
        Assert.DoesNotContain("Finance", departments);
    }

    [Fact]
    /// <summary>
    /// Runs the EmployeeControllerIndex_GlobalUserSeesAllCompanies operation.
    /// </summary>
    public async Task EmployeeControllerIndex_GlobalUserSeesAllCompanies()
    {
        await using var context = CreateContext();
        context.OnboardingRequests.AddRange(
            new OnboardingRequest
            {
                Id = 3,
                FullName = "Alpha Employee",
                EmployeeID = "E-ALPHA",
                Department = "IT",
                Branch = "Alpha",
                Status = RequestStatus.Completed
            },
            new OnboardingRequest
            {
                Id = 4,
                FullName = "Beta Employee",
                EmployeeID = "E-BETA",
                Department = "Finance",
                Branch = "Beta",
                Status = RequestStatus.Completed
            });
        await context.SaveChangesAsync();

        var controller = new EmployeeController(context)
        {
            ControllerContext = CreateControllerContext(scopedToCompany: null)
        };

        var result = await controller.Index(searchString: "", department: "", branch: "", page: 1);

        var viewResult = Assert.IsType<ViewResult>(result);
        var roster = Assert.IsType<PaginatedList<OnboardingRequest>>(viewResult.Model);
        Assert.Equal(2, roster.Count);
    }

    /// <summary>
    /// Creates data for the CreateControllerContext operation.
    /// </summary>
    private static ControllerContext CreateControllerContext(string? scopedToCompany)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "test-user")
        };

        if (!string.IsNullOrWhiteSpace(scopedToCompany))
        {
            claims.Add(new Claim("UserCompany", scopedToCompany));
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"));
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    /// <summary>
    /// Creates data for the CreateContext operation.
    /// </summary>
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var logger = new Mock<ILogger<ApplicationDbContext>>();
        var encryption = new Mock<ILicenseEncryptionService>();
        encryption.Setup(service => service.EncryptKey(It.IsAny<string>())).Returns((string value) => value);
        encryption.Setup(service => service.DecryptKey(It.IsAny<string>())).Returns((string value) => value);

        return new ApplicationDbContext(options, httpContextAccessor.Object, logger.Object, encryption.Object);
    }
}
