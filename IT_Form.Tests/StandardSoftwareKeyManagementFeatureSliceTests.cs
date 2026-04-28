// Project comment: StandardSoftwareKeyManagementFeatureSliceTests.cs is part of the IT Form application source code.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IT_Form.Application.Features.StandardSoftware.AddSoftwareKey;
using IT_Form.Application.Features.StandardSoftware.AssignSoftwareKey;
using IT_Form.Application.Features.StandardSoftware.DeleteSoftwareKey;
using IT_Form.Application.Features.StandardSoftware.QuickAllocateSubscriptionSeat;
using IT_Form.Application.Features.StandardSoftware.UpdateSoftwareKey;
using IT_Form.Data;
using IT_Form.Infrastructure.Notifications;
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
/// Defines the StandardSoftwareKeyManagementFeatureSliceTests class used by the IT Form application.
/// </summary>
public class StandardSoftwareKeyManagementFeatureSliceTests
{
    [Fact]
    /// <summary>
    /// Runs the AddSoftwareKeyValidator_Fails_WhenKeyValueMissing operation.
    /// </summary>
    public void AddSoftwareKeyValidator_Fails_WhenKeyValueMissing()
    {
        var validator = new AddSoftwareKeyCommandValidator();

        var result = validator.Validate(new AddSoftwareKeyCommand(10, string.Empty));

        Assert.False(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Runs the AddSoftwareKeyHandler_ReturnsFailed_WhenImportFails operation.
    /// </summary>
    public async Task AddSoftwareKeyHandler_ReturnsFailed_WhenImportFails()
    {
        var licenseService = new Mock<ILicenseService>();
        licenseService
            .Setup(x => x.ImportKeysAsync(10, It.IsAny<List<string>>()))
            .ReturnsAsync((false, "Import failed.", 0));

        var handler = new AddSoftwareKeyCommandHandler(licenseService.Object);
        var result = await handler.Handle(new AddSoftwareKeyCommand(10, "abc"), CancellationToken.None);

        Assert.Equal(AddSoftwareKeyStatus.Failed, result.Status);
    }

    [Fact]
    /// <summary>
    /// Deletes data for the DeleteSoftwareKeyHandler_ReturnsAssignedBlocked_WhenKeyAssigned operation.
    /// </summary>
    public async Task DeleteSoftwareKeyHandler_ReturnsAssignedBlocked_WhenKeyAssigned()
    {
        using var context = CreateContext();
        context.SoftwareLicenses.Add(new SoftwareLicense
        {
            Id = 1,
            StandardSoftwareId = 10,
            SoftwareName = "Office E3",
            Keys =
            [
                new SoftwareKey
                {
                    Id = 5,
                    SoftwareLicenseId = 1,
                    IsAssigned = true,
                    KeyValue = "k1"
                }
            ]
        });
        await context.SaveChangesAsync();

        var handler = new DeleteSoftwareKeyCommandHandler(context);
        var result = await handler.Handle(new DeleteSoftwareKeyCommand(5), CancellationToken.None);

        Assert.Equal(DeleteSoftwareKeyStatus.AssignedBlocked, result.Status);
    }

    [Fact]
    /// <summary>
    /// Runs the QuickAllocateSeatHandler_ReturnsNotSubscription_WhenLicenseIsPerpetual operation.
    /// </summary>
    public async Task QuickAllocateSeatHandler_ReturnsNotSubscription_WhenLicenseIsPerpetual()
    {
        using var context = CreateContext();
        context.SoftwareLicenses.Add(new SoftwareLicense
        {
            Id = 7,
            StandardSoftwareId = 1,
            SoftwareName = "Perpetual Product",
            IsSubscription = false
        });
        await context.SaveChangesAsync();

        var encryption = new Mock<ILicenseEncryptionService>();
        encryption.Setup(x => x.EncryptKey(It.IsAny<string>())).Returns((string s) => s);
        var handler = new QuickAllocateSubscriptionSeatCommandHandler(context, encryption.Object);

        var result = await handler.Handle(new QuickAllocateSubscriptionSeatCommand(7), CancellationToken.None);

        Assert.Equal(QuickAllocateSubscriptionSeatStatus.NotSubscription, result.Status);
    }

    [Fact]
    /// <summary>
    /// Runs the QuickAllocateSeatHandler_IncrementsSeatLimitAndAddsSeatPlaceholder operation.
    /// </summary>
    public async Task QuickAllocateSeatHandler_IncrementsSeatLimitAndAddsSeatPlaceholder()
    {
        using var context = CreateContext();
        var license = new SoftwareLicense
        {
            Id = 8,
            StandardSoftwareId = 1,
            SoftwareName = "Office E3",
            IsSubscription = true,
            ExpiryDate = DateTime.UtcNow.AddDays(30),
            ContractLimit = 1
        };
        license.Keys.Add(new SoftwareKey
        {
            Id = 9,
            SoftwareLicenseId = 8,
            KeyValue = "Seat-1001",
            IsAssigned = false
        });
        context.SoftwareLicenses.Add(license);
        await context.SaveChangesAsync();

        var encryption = new Mock<ILicenseEncryptionService>();
        encryption.Setup(x => x.EncryptKey(It.IsAny<string>())).Returns((string s) => s);
        var handler = new QuickAllocateSubscriptionSeatCommandHandler(context, encryption.Object);

        var result = await handler.Handle(new QuickAllocateSubscriptionSeatCommand(8), CancellationToken.None);

        Assert.Equal(QuickAllocateSubscriptionSeatStatus.Allocated, result.Status);

        var updatedLicense = await context.SoftwareLicenses.Include(item => item.Keys).SingleAsync(item => item.Id == 8);
        Assert.Equal(2, updatedLicense.ContractLimit);
        Assert.Equal(2, updatedLicense.Keys.Count);
    }

    [Fact]
    /// <summary>
    /// Updates data for the UpdateSoftwareKeyHandler_ReturnsNotFound_WhenKeyMissing operation.
    /// </summary>
    public async Task UpdateSoftwareKeyHandler_ReturnsNotFound_WhenKeyMissing()
    {
        using var context = CreateContext();
        var encryption = new Mock<ILicenseEncryptionService>();
        encryption.Setup(x => x.EncryptKey(It.IsAny<string>())).Returns((string s) => s);

        var handler = new UpdateSoftwareKeyCommandHandler(context, encryption.Object);
        var result = await handler.Handle(new UpdateSoftwareKeyCommand(999, "new-key"), CancellationToken.None);

        Assert.Equal(UpdateSoftwareKeyStatus.NotFound, result.Status);
    }

    [Fact]
    /// <summary>
    /// Runs the AssignSoftwareKeyHandler_ReturnsEmployeeNotFound_WhenEmployeeMissing operation.
    /// </summary>
    public async Task AssignSoftwareKeyHandler_ReturnsEmployeeNotFound_WhenEmployeeMissing()
    {
        using var context = CreateContext();
        context.SoftwareLicenses.Add(new SoftwareLicense
        {
            Id = 20,
            StandardSoftwareId = 1,
            SoftwareName = "Office E3",
            IsSubscription = false
        });
        context.SoftwareKeys.Add(new SoftwareKey
        {
            Id = 21,
            SoftwareLicenseId = 20,
            IsAssigned = false,
            KeyValue = "k2"
        });
        await context.SaveChangesAsync();

        var encryption = new Mock<ILicenseEncryptionService>();
        encryption.Setup(x => x.EncryptKey(It.IsAny<string>())).Returns((string s) => s);
        var queue = new Mock<IEmailBackgroundQueue>();

        var handler = new AssignSoftwareKeyCommandHandler(context, encryption.Object, queue.Object);
        var result = await handler.Handle(new AssignSoftwareKeyCommand(21, 12345), CancellationToken.None);

        Assert.Equal(AssignSoftwareKeyStatus.EmployeeNotFound, result.Status);
    }

    [Fact]
    /// <summary>
    /// Deletes data for the DeleteSoftwareKeyHandler_DecrementsSeatLimit_WhenDeletingUnassignedSubscriptionSeat operation.
    /// </summary>
    public async Task DeleteSoftwareKeyHandler_DecrementsSeatLimit_WhenDeletingUnassignedSubscriptionSeat()
    {
        using var context = CreateContext();
        var license = new SoftwareLicense
        {
            Id = 40,
            StandardSoftwareId = 1,
            SoftwareName = "Office E3",
            IsSubscription = true,
            ContractLimit = 2,
            TotalLicenses = 2
        };
        license.Keys.Add(new SoftwareKey { Id = 41, SoftwareLicenseId = 40, KeyValue = "Seat-2001", IsAssigned = false });
        license.Keys.Add(new SoftwareKey { Id = 42, SoftwareLicenseId = 40, KeyValue = "Seat-2002", IsAssigned = false });
        context.SoftwareLicenses.Add(license);
        await context.SaveChangesAsync();

        var handler = new DeleteSoftwareKeyCommandHandler(context);
        var result = await handler.Handle(new DeleteSoftwareKeyCommand(41), CancellationToken.None);

        Assert.Equal(DeleteSoftwareKeyStatus.Deleted, result.Status);

        context.ChangeTracker.Clear();
        var updatedLicense = await context.SoftwareLicenses.Include(item => item.Keys).SingleAsync(item => item.Id == 40);
        Assert.Equal(1, updatedLicense.ContractLimit);
        Assert.Equal(1, updatedLicense.TotalLicenses);
        Assert.Single(updatedLicense.Keys);
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
