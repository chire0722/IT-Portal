using IT_Form.Application.Common.Mapping;
using IT_Form.Application.Features.UserManagement;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Services;
using IT_Form.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IT_Form.Tests;

public class UserManagementPageQueryTests
{
    public UserManagementPageQueryTests()
    {
        MapsterConfig.RegisterMappings();
    }

    [Fact]
    public async Task IndexQuery_ReturnsTypedItemsWithTitlesAndCompanies()
    {
        await using var context = CreateContext();

        context.Users.Add(new ApplicationUser
        {
            Id = "u1",
            UserName = "alice",
            FullName = "Alice Tan",
            Email = "alice@test.local",
            Department = "IT",
            IsActive = true,
            UserTitles =
            [
                new UserTitle { Title = "HR" },
                new UserTitle { Title = "IT" }
            ],
            UserCompanies =
            [
                new UserCompany { CompanyName = "Alpha" },
                new UserCompany { CompanyName = "Beta" }
            ]
        });
        await context.SaveChangesAsync();

        var handler = new GetUserManagementIndexPageQueryHandler(context, new TestCurrentUserAccessor());

        var result = await handler.Handle(new GetUserManagementIndexPageQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Data!);
        Assert.Equal("u1", item.Id);
        Assert.Equal("Alice Tan", item.FullName);
        Assert.Equal("alice", item.Username);
        Assert.Equal("alice@test.local", item.Email);
        Assert.Equal("IT", item.Department);
        Assert.Equal(["HR", "IT"], item.Titles);
        Assert.Equal(["Alpha", "Beta"], item.Companies);
    }

    [Fact]
    public async Task CreatePageQuery_ReturnsEditorPageWithActiveCompaniesAndPreservesModel()
    {
        await using var context = CreateContext();

        context.Companies.AddRange(
            new Company { Id = 1, Name = "Beta", Lot = "L2", IsActive = true },
            new Company { Id = 2, Name = "Alpha", Lot = "L1", IsActive = true },
            new Company { Id = 3, Name = "Inactive", IsActive = false });
        await context.SaveChangesAsync();

        var handler = new GetUserManagementCreatePageQueryHandler(context, new TestCurrentUserAccessor());
        var form = new UserManagementViewModel
        {
            FullName = "Draft User",
            Username = "draft",
            Email = "draft@test.local",
            SelectedCompanies = ["Alpha (L1)"]
        };

        var result = await handler.Handle(new GetUserManagementCreatePageQuery(form), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var model = Assert.IsType<UserManagementEditorPageViewModel>(result.Data);
        Assert.Equal("Draft User", model.FullName);
        Assert.Equal("draft", model.Username);
        Assert.Equal("draft@test.local", model.Email);
        Assert.Equal(["Alpha (L1)"], model.SelectedCompanies);
        Assert.Equal(["Alpha (L1)", "Beta (L2)"], model.Companies.Select(c => c.DisplayName).ToList());
    }

    [Fact]
    public async Task EditPageQuery_LoadsExistingUserAndReturnsNotFoundWhenMissing()
    {
        await using var context = CreateContext();

        context.Users.Add(new ApplicationUser
        {
            Id = "u9",
            UserName = "manager",
            FullName = "Manager User",
            Email = "manager@test.local",
            Department = "Operations",
            IsActive = true,
            UserTitles =
            [
                new UserTitle { Title = "HOD" }
            ],
            UserCompanies =
            [
                new UserCompany { CompanyName = "Gamma" }
            ]
        });
        context.Companies.Add(new Company { Id = 9, Name = "Gamma", IsActive = true });
        await context.SaveChangesAsync();

        var handler = new GetUserManagementEditPageQueryHandler(context, new TestCurrentUserAccessor());

        var missing = await handler.Handle(new GetUserManagementEditPageQuery("missing"), CancellationToken.None);
        Assert.True(missing.IsNotFound);

        var result = await handler.Handle(new GetUserManagementEditPageQuery("u9"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var model = Assert.IsType<UserManagementEditorPageViewModel>(result.Data);
        Assert.Equal("u9", model.Id);
        Assert.Equal("Manager User", model.FullName);
        Assert.Equal("manager", model.Username);
        Assert.Equal("manager@test.local", model.Email);
        Assert.Equal("Operations", model.Department);
        Assert.True(model.IsHOD);
        Assert.Equal(["Gamma"], model.SelectedCompanies);
        Assert.Equal(["Gamma"], model.Companies.Select(c => c.DisplayName).ToList());
    }

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
