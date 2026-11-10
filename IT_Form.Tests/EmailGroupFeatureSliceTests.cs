// Project comment: EmailGroupFeatureSliceTests.cs is part of the IT Form application source code.

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using IT_Form.Application.Common.Exceptions;
using IT_Form.Application.Common.Mapping;
using IT_Form.Application.Features.EmailGroup.AddCurrentEmployeeToEmailGroup;
using IT_Form.Application.Features.EmailGroup.ImportEmailGroups;
using IT_Form.Application.Features.EmailGroups.Writes.CreateEmailGroup;
using IT_Form.Application.Features.EmailGroups.Writes.DeleteEmailGroup;
using IT_Form.Application.Features.EmailGroups.Writes.UpdateEmailGroup;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IT_Form.Tests;

/// <summary>
/// Defines the EmailGroupFeatureSliceTests class used by the IT Form application.
/// </summary>
public class EmailGroupFeatureSliceTests
{
    public EmailGroupFeatureSliceTests()
    {
        MapsterConfig.RegisterMappings();
    }

    [Fact]
    /// <summary>
    /// Creates data for the CreateEmailGroupValidator_Fails_WhenEmailInvalid operation.
    /// </summary>
    public async Task CreateEmailGroupValidator_Fails_WhenEmailInvalid()
    {
        using var context = CreateContext();
        var validator = new CreateEmailGroupCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateEmailGroupCommand("All Staff", "not-email", true, Array.Empty<int>()));

        Assert.False(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Creates data for the CreateEmailGroupHandler_AllowsGlobalAccessUserToCreateGlobalEmailGroup operation.
    /// </summary>
    public async Task CreateEmailGroupHandler_AllowsGlobalAccessUserToCreateGlobalEmailGroup()
    {
        using var context = CreateContext();

        var handler = new CreateEmailGroupCommandHandler(
            context,
            new TestCurrentUserAccessor(titles: ["IT"]));

        var id = await handler.Handle(
            new CreateEmailGroupCommand("All Staff", "all.staff@test.local", true, []),
            CancellationToken.None);

        var entity = await context.EmailGroups
            .Include(group => group.EmailGroupCompanies)
            .SingleAsync(group => group.Id == id);

        Assert.Equal("All Staff", entity.GroupName);
        Assert.Empty(entity.EmailGroupCompanies);
    }

    [Fact]
    /// <summary>
    /// Updates data for the UpdateEmailGroupHandler_ThrowsNotFound_WhenGroupMissing operation.
    /// </summary>
    public async Task UpdateEmailGroupHandler_ThrowsNotFound_WhenGroupMissing()
    {
        using var context = CreateContext();
        var handler = new UpdateEmailGroupCommandHandler(context, new TestCurrentUserAccessor());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateEmailGroupCommand(999, "A", "a@test.com", true, Array.Empty<int>()), CancellationToken.None));
    }

    [Fact]
    /// <summary>
    /// Deletes data for the DeleteEmailGroupHandler_Deletes_WhenFound operation.
    /// </summary>
    public async Task DeleteEmailGroupHandler_Deletes_WhenFound()
    {
        using var context = CreateContext();
        context.EmailGroups.Add(new EmailGroup { Id = 1, GroupName = "G", GroupEmail = "g@test.com", IsActive = true });
        await context.SaveChangesAsync();

        var handler = new DeleteEmailGroupCommandHandler(context, new TestCurrentUserAccessor());

        await handler.Handle(new DeleteEmailGroupCommand(1), CancellationToken.None);

        Assert.Null(await context.EmailGroups.FindAsync(1));
    }

    [Fact]
    /// <summary>
    /// Runs the AddCurrentEmployeeHandler_ReturnsAlreadyExists_WhenAlreadyAssigned operation.
    /// </summary>
    public async Task AddCurrentEmployeeHandler_ReturnsAlreadyExists_WhenAlreadyAssigned()
    {
        using var context = CreateContext();

        context.EmailGroups.Add(new EmailGroup { Id = 11, GroupName = "All Staff", GroupEmail = "all@test.com", IsActive = true });
        context.OnboardingRequests.Add(new OnboardingRequest { Id = 22, FullName = "User", Status = RequestStatus.Completed });
        context.RequestEmailGroups.Add(new RequestEmailGroup { EmailGroupId = 11, OnboardingRequestId = 22, GroupName = "All Staff", GroupEmail = "all@test.com" });
        await context.SaveChangesAsync();

        var handler = new AddCurrentEmployeeToEmailGroupCommandHandler(context, new TestCurrentUserAccessor());

        var result = await handler.Handle(new AddCurrentEmployeeToEmailGroupCommand(11, 22), CancellationToken.None);

        Assert.Equal(AddCurrentEmployeeToEmailGroupStatus.AlreadyExists, result.Status);
    }

    [Fact]
    /// <summary>
    /// Runs the AddCurrentEmployeeHandler_RejectsCrossCompanyGroupAndEmployee operation.
    /// </summary>
    public async Task AddCurrentEmployeeHandler_RejectsCrossCompanyGroupAndEmployee()
    {
        using var context = CreateContext();

        context.Companies.AddRange(
            new Company { Id = 1, Name = "Alpha", IsActive = true },
            new Company { Id = 2, Name = "Beta", IsActive = true });
        context.EmailGroups.Add(new EmailGroup
        {
            Id = 12,
            GroupName = "Beta Group",
            GroupEmail = "beta.group@test.local",
            IsActive = true,
            EmailGroupCompanies = [new EmailGroupCompany { CompanyId = 2 }]
        });
        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 23,
            FullName = "Beta User",
            Branch = "Beta",
            Status = RequestStatus.Completed
        });
        await context.SaveChangesAsync();

        var handler = new AddCurrentEmployeeToEmailGroupCommandHandler(
            context,
            new TestCurrentUserAccessor(titles: ["IT"], companies: ["Alpha"]));

        var result = await handler.Handle(new AddCurrentEmployeeToEmailGroupCommand(12, 23), CancellationToken.None);

        Assert.Equal(AddCurrentEmployeeToEmailGroupStatus.Forbidden, result.Status);
        Assert.False(await context.RequestEmailGroups.AnyAsync(link => link.EmailGroupId == 12 && link.OnboardingRequestId == 23));
    }

    [Fact]
    /// <summary>
    /// Runs the ImportEmailGroupsValidator_Fails_WhenFileMissing operation.
    /// </summary>
    public void ImportEmailGroupsValidator_Fails_WhenFileMissing()
    {
        var validator = new ImportEmailGroupsCommandValidator();

        var result = validator.Validate(new ImportEmailGroupsCommand(null!));

        Assert.False(result.IsValid);
    }

    /// <summary>
    /// Creates data for the CreateContext operation.
    /// </summary>
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var http = new Mock<IHttpContextAccessor>();
        var logger = new Mock<ILogger<ApplicationDbContext>>();
        var encryption = new Mock<ILicenseEncryptionService>();
        encryption.Setup(x => x.EncryptKey(It.IsAny<string>())).Returns((string s) => s);
        encryption.Setup(x => x.DecryptKey(It.IsAny<string>())).Returns((string s) => s);

        return new ApplicationDbContext(options, http.Object, logger.Object, encryption.Object);
    }
}
