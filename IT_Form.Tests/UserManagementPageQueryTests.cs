// Project comment: UserManagementPageQueryTests.cs is part of the IT Form application source code.

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

/// <summary>
/// Defines the UserManagementPageQueryTests class used by the IT Form application.
/// </summary>
public class UserManagementPageQueryTests
{
    public UserManagementPageQueryTests()
    {
        MapsterConfig.RegisterMappings();
    }

    [Fact]
    /// <summary>
    /// Runs the IndexQuery_ReturnsTypedItemsWithTitlesAndCompanies operation.
    /// </summary>
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
    public async Task IndexAndEdit_HideSuperAdminUser_ForItUsers()
    {
        await using var context = CreateContext();
        context.Users.AddRange(
            new ApplicationUser
            {
                Id = "regular",
                UserName = "regular",
                FullName = "Regular User",
                Email = "regular@test.local",
                IsActive = true,
                UserTitles = [new UserTitle { Title = "IT" }]
            },
            new ApplicationUser
            {
                Id = "super",
                UserName = "super",
                FullName = "Super Admin",
                Email = "super@test.local",
                IsActive = true,
                UserTitles =
                [
                    new UserTitle { Title = "IT" },
                    new UserTitle { Title = "SuperAdmin" }
                ]
            });
        await context.SaveChangesAsync();

        var itAccessor = new TestCurrentUserAccessor(titles: ["IT"]);
        var itIndexHandler = new GetUserManagementIndexPageQueryHandler(context, itAccessor);
        var itEditHandler = new GetUserManagementEditPageQueryHandler(context, itAccessor);

        var itIndexResult = await itIndexHandler.Handle(new GetUserManagementIndexPageQuery(), CancellationToken.None);
        var itEditResult = await itEditHandler.Handle(new GetUserManagementEditPageQuery("super"), CancellationToken.None);

        Assert.True(itIndexResult.IsSuccess);
        var visibleToIt = Assert.IsType<List<UserManagementIndexListItem>>(itIndexResult.Data);
        var visibleItem = Assert.Single(visibleToIt);
        Assert.Equal("regular", visibleItem.Id);
        Assert.True(itEditResult.IsNotFound);

        var superAccessor = new TestCurrentUserAccessor(titles: ["SuperAdmin"]);
        var superIndexHandler = new GetUserManagementIndexPageQueryHandler(context, superAccessor);
        var superEditHandler = new GetUserManagementEditPageQueryHandler(context, superAccessor);

        var superIndexResult = await superIndexHandler.Handle(new GetUserManagementIndexPageQuery(), CancellationToken.None);
        var superEditResult = await superEditHandler.Handle(new GetUserManagementEditPageQuery("super"), CancellationToken.None);

        Assert.True(superIndexResult.IsSuccess);
        Assert.Contains(superIndexResult.Data!, item => item.Id == "super");
        Assert.True(superEditResult.IsSuccess);
        Assert.True(superEditResult.Data!.IsSuperAdmin);
    }

    [Fact]
    /// <summary>
    /// Creates data for the CreatePageQuery_ReturnsEditorPageWithActiveCompaniesAndPreservesModel operation.
    /// </summary>
    public async Task CreatePageQuery_ReturnsEditorPageWithActiveCompaniesAndPreservesModel()
    {
        await using var context = CreateContext();

        context.Departments.AddRange(
            new Department { Id = 1, Name = "Sales", IsActive = true },
            new Department { Id = 2, Name = "IT", IsActive = true },
            new Department { Id = 3, Name = "Dormant", IsActive = false });
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
        Assert.Equal(["IT", "Sales"], model.Departments.Select(d => d.Name).ToList());
        Assert.Equal(["Alpha (L1)", "Beta (L2)"], model.Companies.Select(c => c.DisplayName).ToList());
    }

    [Fact]
    /// <summary>
    /// Runs the EditPageQuery_LoadsExistingUserAndReturnsNotFoundWhenMissing operation.
    /// </summary>
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
        context.Departments.AddRange(
            new Department { Id = 9, Name = "Operations", IsActive = true },
            new Department { Id = 10, Name = "Inactive", IsActive = false });
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
        Assert.Equal(["Operations"], model.Departments.Select(d => d.Name).ToList());
        Assert.Equal(["Gamma"], model.SelectedCompanies);
        Assert.Equal(["Gamma"], model.Companies.Select(c => c.DisplayName).ToList());
    }

    [Fact]
    /// <summary>
    /// Runs the ItUserWithAssignedCompany_SeesAndEditsUsersAcrossCompanies operation.
    /// </summary>
    public async Task ItUserWithAssignedCompany_SeesAndEditsUsersAcrossCompanies()
    {
        await using var context = CreateContext();

        context.Departments.AddRange(
            new Department
            {
                Id = 11,
                Name = "Alpha Only",
                IsActive = true,
                DepartmentCompanies = [new DepartmentCompany { CompanyId = 1 }]
            },
            new Department
            {
                Id = 12,
                Name = "Global Dept",
                IsActive = true
            },
            new Department
            {
                Id = 13,
                Name = "Beta Only",
                IsActive = true,
                DepartmentCompanies = [new DepartmentCompany { CompanyId = 2 }]
            });
        context.Companies.AddRange(
            new Company { Id = 1, Name = "Alpha", IsActive = true },
            new Company { Id = 2, Name = "Beta", IsActive = true });
        context.Users.AddRange(
            new ApplicationUser
            {
                Id = "u-alpha",
                UserName = "alpha.user",
                FullName = "Alpha User",
                Email = "alpha@test.local",
                IsActive = true,
                UserCompanies = [new UserCompany { CompanyName = "Alpha" }]
            },
            new ApplicationUser
            {
                Id = "u-beta",
                UserName = "beta.user",
                FullName = "Beta User",
                Email = "beta@test.local",
                IsActive = true,
                UserCompanies = [new UserCompany { CompanyName = "Beta" }]
            });
        await context.SaveChangesAsync();

        var currentUser = new TestCurrentUserAccessor(
            userName: "it.user",
            titles: ["IT"],
            companies: ["Alpha"]);
        var indexHandler = new GetUserManagementIndexPageQueryHandler(context, currentUser);
        var createHandler = new GetUserManagementCreatePageQueryHandler(context, currentUser);
        var editHandler = new GetUserManagementEditPageQueryHandler(context, currentUser);

        var indexResult = await indexHandler.Handle(new GetUserManagementIndexPageQuery(), CancellationToken.None);
        var createResult = await createHandler.Handle(new GetUserManagementCreatePageQuery(), CancellationToken.None);
        var editResult = await editHandler.Handle(new GetUserManagementEditPageQuery("u-beta"), CancellationToken.None);

        Assert.True(indexResult.IsSuccess);
        var scopedUsers = Assert.IsType<List<UserManagementIndexListItem>>(indexResult.Data);
        Assert.Equal(2, scopedUsers.Count);
        Assert.Contains(scopedUsers, item => item.Id == "u-alpha");
        Assert.Contains(scopedUsers, item => item.Id == "u-beta");

        Assert.True(createResult.IsSuccess);
        Assert.Equal(["Alpha Only", "Beta Only", "Global Dept"], createResult.Data!.Departments.Select(department => department.Name).ToList());
        Assert.Equal(["Alpha", "Beta"], createResult.Data!.Companies.Select(company => company.DisplayName).ToList());

        Assert.True(editResult.IsSuccess);
        Assert.Equal("u-beta", editResult.Data!.Id);
        Assert.Equal(["Beta"], editResult.Data.SelectedCompanies);
        Assert.Equal(["Alpha", "Beta"], editResult.Data.Companies.Select(company => company.DisplayName).ToList());
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
