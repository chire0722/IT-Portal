using System;
using System.Threading;
using System.Threading.Tasks;
using IT_Form.Application.Features.EmailContact.BulkImportEmailContacts;
using IT_Form.Application.Common.Exceptions;
using IT_Form.Application.Features.EmailContact.Writes.CreateEmailContact;
using IT_Form.Application.Features.EmailContact.Writes.DeleteEmailContact;
using IT_Form.Application.Features.EmailContact.Writes.UpdateEmailContact;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IT_Form.Tests;

public class EmailContactFeatureSliceTests
{
    [Fact]
    public void CreateEmailContactValidator_Fails_WhenEmailInvalid()
    {
        var validator = new CreateEmailContactCommandValidator();

        var result = validator.Validate(new CreateEmailContactCommand("John", "bad-email", "IT"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task EditEmailContactHandler_ReturnsNotFound_WhenContactMissing()
    {
        using var context = CreateContext();
        var handler = new UpdateEmailContactCommandHandler(context);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateEmailContactCommand(999, "John", "john@test.com", "IT"), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteEmailContactHandler_Deletes_WhenFound()
    {
        using var context = CreateContext();
        context.EmailContacts.Add(new EmailContact { Id = 1, Name = "John", EmailAddress = "john@test.com" });
        await context.SaveChangesAsync();

        var handler = new DeleteEmailContactCommandHandler(context);
        await handler.Handle(new DeleteEmailContactCommand(1), CancellationToken.None);

        Assert.Null(await context.EmailContacts.FindAsync(1));
    }

    [Fact]
    public void BulkImportEmailContactsValidator_Fails_WhenFileMissing()
    {
        var validator = new BulkImportEmailContactsCommandValidator();

        var result = validator.Validate(new BulkImportEmailContactsCommand(null!));

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
