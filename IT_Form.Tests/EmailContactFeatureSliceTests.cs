// Project comment: EmailContactFeatureSliceTests.cs is part of the IT Form application source code.

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

/// <summary>
/// Defines the EmailContactFeatureSliceTests class used by the IT Form application.
/// </summary>
public class EmailContactFeatureSliceTests
{
    [Fact]
    /// <summary>
    /// Creates data for the CreateEmailContactValidator_Fails_WhenEmailInvalid operation.
    /// </summary>
    public void CreateEmailContactValidator_Fails_WhenEmailInvalid()
    {
        var validator = new CreateEmailContactCommandValidator();

        var result = validator.Validate(new CreateEmailContactCommand("John", "bad-email", "IT"));

        Assert.False(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Runs the EditEmailContactHandler_ReturnsNotFound_WhenContactMissing operation.
    /// </summary>
    public async Task EditEmailContactHandler_ReturnsNotFound_WhenContactMissing()
    {
        using var context = CreateContext();
        var handler = new UpdateEmailContactCommandHandler(context);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateEmailContactCommand(999, "John", "john@test.com", "IT"), CancellationToken.None));
    }

    [Fact]
    /// <summary>
    /// Deletes data for the DeleteEmailContactHandler_Deletes_WhenFound operation.
    /// </summary>
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
    /// <summary>
    /// Runs the BulkImportEmailContactsValidator_Fails_WhenFileMissing operation.
    /// </summary>
    public void BulkImportEmailContactsValidator_Fails_WhenFileMissing()
    {
        var validator = new BulkImportEmailContactsCommandValidator();

        var result = validator.Validate(new BulkImportEmailContactsCommand(null!));

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
