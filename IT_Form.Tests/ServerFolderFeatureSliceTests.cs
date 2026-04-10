using System;
using System.Threading;
using System.Threading.Tasks;
using IT_Form.Application.Common.Exceptions;
using IT_Form.Application.Features.ServerFolder.AddCurrentEmployeeToServerFolder;
using IT_Form.Application.Features.ServerFolder.BulkImportServerFolders;
using IT_Form.Application.Features.ServerFolders.Writes.CreateServerFolder;
using IT_Form.Application.Features.ServerFolders.Writes.UpdateServerFolder;
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

public class ServerFolderFeatureSliceTests
{
    [Fact]
    public async Task CreateServerFolderValidator_Fails_WhenNameMissing()
    {
        using var context = CreateContext();
        var validator = new CreateServerFolderCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateServerFolderCommand(string.Empty, true, Array.Empty<int>()));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task UpdateServerFolderHandler_ThrowsNotFound_WhenFolderMissing()
    {
        using var context = CreateContext();
        var handler = new UpdateServerFolderCommandHandler(context, new TestCurrentUserAccessor());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateServerFolderCommand(999, "Public", true, Array.Empty<int>()), CancellationToken.None));
    }

    [Fact]
    public async Task AddCurrentEmployeeFolderHandler_ReturnsUpdated_WhenPermissionExists()
    {
        using var context = CreateContext();

        context.ServerFolders.Add(new ServerFolder { Id = 11, FolderName = "Public", IsActive = true });
        context.OnboardingRequests.Add(new OnboardingRequest { Id = 22, FullName = "User", Status = RequestStatus.Completed });
        context.FolderPermissions.Add(new FolderPermission { OnboardingRequestId = 22, FolderName = "Public", CanRead = true, CanModify = false });
        await context.SaveChangesAsync();

        var handler = new AddCurrentEmployeeToServerFolderCommandHandler(context, new TestCurrentUserAccessor());

        var result = await handler.Handle(new AddCurrentEmployeeToServerFolderCommand(11, 22, CanRead: false, CanModify: true), CancellationToken.None);

        Assert.Equal(AddCurrentEmployeeToServerFolderStatus.Updated, result.Status);
    }

    [Fact]
    public void BulkImportServerFoldersValidator_Fails_WhenFileMissing()
    {
        var validator = new BulkImportServerFoldersCommandValidator();

        var result = validator.Validate(new BulkImportServerFoldersCommand(null!));

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
