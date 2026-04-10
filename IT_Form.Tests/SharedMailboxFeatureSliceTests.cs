using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using IT_Form.Application.Common.Exceptions;
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

public class SharedMailboxFeatureSliceTests
{
    [Fact]
    public async Task CreateSharedMailboxValidator_Fails_WhenEmailInvalid()
    {
        using var context = CreateContext();
        var validator = new CreateSharedMailboxCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateSharedMailboxCommand("Sales", "invalid-email", true, Array.Empty<int>()));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task UpdateSharedMailboxHandler_ThrowsNotFound_WhenMailboxMissing()
    {
        using var context = CreateContext();
        var handler = new UpdateSharedMailboxCommandHandler(context, new TestCurrentUserAccessor());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateSharedMailboxCommand(999, "A", "a@test.com", true, Array.Empty<int>()), CancellationToken.None));
    }

    [Fact]
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
    public void ImportSharedMailboxesValidator_Fails_WhenFileMissing()
    {
        var validator = new ImportSharedMailboxesCommandValidator();

        var result = validator.Validate(new ImportSharedMailboxesCommand(null!));

        Assert.False(result.IsValid);
    }

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
