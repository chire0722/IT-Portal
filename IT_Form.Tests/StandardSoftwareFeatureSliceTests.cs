using System;
using System.Threading;
using System.Threading.Tasks;
using IT_Form.Application.Features.StandardSoftware.AddSoftwareLicense;
using IT_Form.Application.Features.StandardSoftware.EditSoftwareLicense;
using IT_Form.Application.Features.StandardSoftware.DeleteSoftwareLicense;
using IT_Form.Application.Features.StandardSoftware.ImportStandardSoftware;
using IT_Form.Application.Common.Exceptions;
using IT_Form.Application.Features.StandardSoftware.Writes.CreateStandardSoftware;
using IT_Form.Application.Features.StandardSoftware.Writes.DeleteStandardSoftware;
using IT_Form.Application.Features.StandardSoftware.Writes.UpdateStandardSoftware;
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

public class StandardSoftwareFeatureSliceTests
{
    [Fact]
    public void CreateStandardSoftwareValidator_Fails_WhenNameMissing()
    {
        var validator = new CreateStandardSoftwareCommandValidator();

        var result = validator.Validate(new CreateStandardSoftwareCommand(string.Empty, false, false, true, false));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task EditStandardSoftwareHandler_ReturnsNotFound_WhenSoftwareMissing()
    {
        using var context = CreateContext();
        var handler = new UpdateStandardSoftwareCommandHandler(context);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateStandardSoftwareCommand(999, "Office", false, false, true, false), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteStandardSoftwareValidator_Fails_WhenAnyAssigned()
    {
        using var context = CreateContext();

        var software = new StandardSoftware { Id = 1, SoftwareName = "Office" };
        var license = new SoftwareLicense { Id = 2, StandardSoftwareId = 1, SoftwareName = "Office E3" };
        var key = new SoftwareKey { Id = 3, SoftwareLicenseId = 2, IsAssigned = true, KeyValue = "abc" };
        license.Keys.Add(key);
        software.Licenses.Add(license);

        context.StandardSoftware.Add(software);
        await context.SaveChangesAsync();

        var validator = new DeleteStandardSoftwareCommandValidator(context);
        var result = await validator.ValidateAsync(new DeleteStandardSoftwareCommand(1, "user1"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task AddSoftwareLicenseHandler_RequiresExpiry_ForPaidSubscription()
    {
        using var context = CreateContext();
        context.StandardSoftware.Add(new StandardSoftware { Id = 10, SoftwareName = "Office" });
        await context.SaveChangesAsync();

        var handler = new AddSoftwareLicenseCommandHandler(context);
        var result = await handler.Handle(new AddSoftwareLicenseCommand(10, "Office E3", null, true, false, null, 10, null, null), CancellationToken.None);

        Assert.Equal(AddSoftwareLicenseStatus.InvalidSubscriptionExpiry, result.Status);
    }

    [Fact]
    public async Task AddSoftwareLicenseHandler_CreatesSeatRows_ForSubscriptionQuantity()
    {
        using var context = CreateContext();
        context.StandardSoftware.Add(new StandardSoftware { Id = 11, SoftwareName = "Microsoft 365" });
        await context.SaveChangesAsync();

        var handler = new AddSoftwareLicenseCommandHandler(context);
        var result = await handler.Handle(
            new AddSoftwareLicenseCommand(
                11,
                "Microsoft 365 Business",
                null,
                true,
                false,
                DateTime.UtcNow.AddDays(30),
                3,
                null,
                null),
            CancellationToken.None);

        Assert.Equal(AddSoftwareLicenseStatus.Added, result.Status);

        var license = await context.SoftwareLicenses.Include(item => item.Keys).SingleAsync();
        Assert.Equal(3, license.ContractLimit);
        Assert.Equal(3, license.Keys.Count);
        Assert.All(license.Keys, key => Assert.False(key.IsAssigned));
    }

    [Fact]
    public async Task EditSoftwareLicenseHandler_ReturnsInvalidSeatLimit_WhenReducingBelowAssignedSeats()
    {
        using var context = CreateContext();
        var license = new SoftwareLicense
        {
            Id = 30,
            StandardSoftwareId = 1,
            SoftwareName = "Office E3",
            IsSubscription = true,
            ExpiryDate = DateTime.UtcNow.AddDays(30),
            ContractLimit = 2
        };
        license.Keys.Add(new SoftwareKey { Id = 31, SoftwareLicenseId = 30, KeyValue = "Seat-a1", IsAssigned = true });
        license.Keys.Add(new SoftwareKey { Id = 32, SoftwareLicenseId = 30, KeyValue = "Seat-a2", IsAssigned = false });
        context.SoftwareLicenses.Add(license);
        await context.SaveChangesAsync();

        var handler = new EditSoftwareLicenseCommandHandler(context);
        var result = await handler.Handle(
            new EditSoftwareLicenseCommand(
                30,
                "Office E3",
                null,
                true,
                false,
                DateTime.UtcNow.AddDays(60),
                0,
                null,
                null),
            CancellationToken.None);

        Assert.Equal(EditSoftwareLicenseStatus.InvalidSeatLimit, result.Status);
        Assert.Contains("assigned seat", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteSoftwareLicenseHandler_ReturnsHasAssignedKeys_WhenAssigned()
    {
        using var context = CreateContext();
        var license = new SoftwareLicense { Id = 20, StandardSoftwareId = 1, SoftwareName = "Office E3" };
        license.Keys.Add(new SoftwareKey { Id = 21, SoftwareLicenseId = 20, IsAssigned = true, KeyValue = "abc" });
        context.SoftwareLicenses.Add(license);
        await context.SaveChangesAsync();

        var handler = new DeleteSoftwareLicenseCommandHandler(context);
        var result = await handler.Handle(new DeleteSoftwareLicenseCommand(20), CancellationToken.None);

        Assert.Equal(DeleteSoftwareLicenseStatus.HasAssignedKeys, result.Status);
    }

    [Fact]
    public void ImportStandardSoftwareValidator_Fails_WhenFileMissing()
    {
        var validator = new ImportStandardSoftwareCommandValidator();

        var result = validator.Validate(new ImportStandardSoftwareCommand(null!));

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
