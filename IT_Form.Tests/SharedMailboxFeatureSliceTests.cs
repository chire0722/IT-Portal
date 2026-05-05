// Project comment: SharedMailboxFeatureSliceTests.cs is part of the IT Form application source code.

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using IT_Form.Application.Common.Exceptions;
using IT_Form.Application.Common.Mapping;
using IT_Form.Application.Features.SharedMailbox.AddCurrentEmployeeToSharedMailbox;
using IT_Form.Application.Features.SharedMailbox.ImportSharedMailboxes;
using IT_Form.Application.Features.SharedMailboxes.Writes.CreateSharedMailbox;
using IT_Form.Application.Features.SharedMailboxes.Writes.DeleteSharedMailbox;
using IT_Form.Application.Features.SharedMailboxes.Writes.UpdateSharedMailbox;
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
/// Defines the SharedMailboxFeatureSliceTests class used by the IT Form application.
/// </summary>
public class SharedMailboxFeatureSliceTests
{
    public SharedMailboxFeatureSliceTests()
    {
        MapsterConfig.RegisterMappings();
    }

    [Fact]
    /// <summary>
    /// Creates data for the CreateSharedMailboxValidator_Fails_WhenEmailInvalid operation.
    /// </summary>
    public async Task CreateSharedMailboxValidator_Fails_WhenEmailInvalid()
    {
        using var context = CreateContext();
        var validator = new CreateSharedMailboxCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateSharedMailboxCommand("Sales", "invalid-email", true, Array.Empty<int>()));

        Assert.False(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Creates data for the CreateSharedMailboxHandler_AllowsGlobalAccessUserToCreateGlobalSharedMailbox operation.
    /// </summary>
    public async Task CreateSharedMailboxHandler_AllowsGlobalAccessUserToCreateGlobalSharedMailbox()
    {
        using var context = CreateContext();

        var handler = new CreateSharedMailboxCommandHandler(
            context,
            new TestCurrentUserAccessor(titles: ["IT"], companies: ["Alpha"]));

        var id = await handler.Handle(
            new CreateSharedMailboxCommand("Global Mailbox", "global.mailbox@test.local", true, []),
            CancellationToken.None);

        var entity = await context.SharedMailboxes
            .Include(mailbox => mailbox.SharedMailboxCompanies)
            .SingleAsync(mailbox => mailbox.Id == id);

        Assert.Equal("Global Mailbox", entity.MailboxName);
        Assert.Empty(entity.SharedMailboxCompanies);
    }

    [Fact]
    /// <summary>
    /// Updates data for the UpdateSharedMailboxHandler_ThrowsNotFound_WhenMailboxMissing operation.
    /// </summary>
    public async Task UpdateSharedMailboxHandler_ThrowsNotFound_WhenMailboxMissing()
    {
        using var context = CreateContext();
        var handler = new UpdateSharedMailboxCommandHandler(context, new TestCurrentUserAccessor());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateSharedMailboxCommand(999, "A", "a@test.com", true, Array.Empty<int>()), CancellationToken.None));
    }

    [Fact]
    /// <summary>
    /// Deletes data for the DeleteSharedMailboxHandler_Deletes_WhenFound operation.
    /// </summary>
    public async Task DeleteSharedMailboxHandler_Deletes_WhenFound()
    {
        using var context = CreateContext();
        context.SharedMailboxes.Add(new SharedMailbox { Id = 1, MailboxName = "M", MailboxAddress = "m@test.com", IsActive = true });
        await context.SaveChangesAsync();

        var handler = new DeleteSharedMailboxCommandHandler(context, new TestCurrentUserAccessor());
        await handler.Handle(new DeleteSharedMailboxCommand(1), CancellationToken.None);

        Assert.Null(await context.SharedMailboxes.FindAsync(1));
    }

    [Fact]
    /// <summary>
    /// Runs the AddCurrentEmployeeSharedMailboxHandler_ReturnsAlreadyExists_WhenAlreadyAssigned operation.
    /// </summary>
    public async Task AddCurrentEmployeeSharedMailboxHandler_ReturnsAlreadyExists_WhenAlreadyAssigned()
    {
        using var context = CreateContext();

        context.SharedMailboxes.Add(new SharedMailbox { Id = 11, MailboxName = "Sales", MailboxAddress = "sales@test.com", IsActive = true });
        context.OnboardingRequests.Add(new OnboardingRequest { Id = 22, FullName = "User", Status = RequestStatus.Completed });
        context.RequestSharedMailboxes.Add(new RequestSharedMailbox { SharedMailboxId = 11, OnboardingRequestId = 22, MailboxName = "Sales", MailboxAddress = "sales@test.com" });
        await context.SaveChangesAsync();

        var handler = new AddCurrentEmployeeToSharedMailboxCommandHandler(context, new TestCurrentUserAccessor());
        var result = await handler.Handle(new AddCurrentEmployeeToSharedMailboxCommand(11, 22), CancellationToken.None);

        Assert.Equal(AddCurrentEmployeeToSharedMailboxStatus.AlreadyExists, result.Status);
    }

    [Fact]
    /// <summary>
    /// Runs the ImportSharedMailboxesValidator_Fails_WhenFileMissing operation.
    /// </summary>
    public void ImportSharedMailboxesValidator_Fails_WhenFileMissing()
    {
        var validator = new ImportSharedMailboxesCommandValidator();

        var result = validator.Validate(new ImportSharedMailboxesCommand(null!));

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
