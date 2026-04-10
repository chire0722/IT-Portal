using System;
using System.Threading;
using System.Threading.Tasks;
using IT_Form.Application.Features.EmployeeTransfer.ConfirmEmployeeTransfer;
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

public class EmployeeTransferFeatureSliceTests
{
    [Fact]
    public void ConfirmValidator_Fails_WhenSignatureMissing()
    {
        var validator = new ConfirmEmployeeTransferCommandValidator();

        var result = validator.Validate(new ConfirmEmployeeTransferCommand(Guid.NewGuid(), string.Empty));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ConfirmHandler_ReturnsExpired_WhenReferenceDateOlderThanSevenDays()
    {
        using var context = CreateContext();
        var token = Guid.NewGuid();
        context.EmployeeTransfers.Add(new EmployeeTransfer
        {
            Id = 21,
            AccessToken = token,
            Status = RequestStatus.PendingEmployeeSign,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-8),
            FullName = "Expired User",
            FromDepartment = "IT",
            ToDepartment = "HR"
        });
        await context.SaveChangesAsync();

        var integrity = new Mock<ISignatureIntegrityService>();
        var handler = new ConfirmEmployeeTransferCommandHandler(context, integrity.Object);
        var result = await handler.Handle(new ConfirmEmployeeTransferCommand(token, "sig"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("This employee confirmation link has expired. Please request a new one.", result.Message);
    }

    [Fact]
    public async Task ConfirmHandler_CompletesTransfer_AndRotatesToken()
    {
        using var context = CreateContext();
        var token = Guid.NewGuid();
        context.EmployeeTransfers.Add(new EmployeeTransfer
        {
            Id = 22,
            AccessToken = token,
            Status = RequestStatus.PendingEmployeeSign,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            FullName = "Transfer User",
            FromDepartment = "IT",
            ToDepartment = "Finance"
        });
        await context.SaveChangesAsync();

        var integrity = new Mock<ISignatureIntegrityService>();
        integrity.Setup(x => x.ComputeHmac(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<string>()))
            .Returns("hmac");

        var handler = new ConfirmEmployeeTransferCommandHandler(context, integrity.Object);
        var result = await handler.Handle(new ConfirmEmployeeTransferCommand(token, "sig"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Transfer acknowledgment completed successfully. Thank you!", result.Message);

        var saved = await context.EmployeeTransfers.FindAsync(22);
        Assert.NotNull(saved);
        Assert.Equal(RequestStatus.Completed, saved!.Status);
        Assert.NotEqual(token, saved.AccessToken);
        Assert.Equal("hmac", saved.EmployeeSignatureHmac);
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
