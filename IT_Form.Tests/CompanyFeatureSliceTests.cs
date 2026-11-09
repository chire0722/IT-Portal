// Project comment: CompanyFeatureSliceTests.cs is part of the IT Form application source code.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IT_Form.Application.Features.Company.SyncCompanyNames;
using IT_Form.Application.Common.Exceptions;
using IT_Form.Application.Features.Company.Writes.CreateCompany;
using IT_Form.Application.Features.Company.Writes.DeleteCompany;
using IT_Form.Application.Features.Company.Writes.UpdateCompany;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IT_Form.Tests;

/// <summary>
/// Defines the CompanyFeatureSliceTests class used by the IT Form application.
/// </summary>
public class CompanyFeatureSliceTests
{
    [Fact]
    /// <summary>
    /// Creates data for the CreateCompanyValidator_Fails_WhenLogoMissing operation.
    /// </summary>
    public void CreateCompanyValidator_Fails_WhenLogoMissing()
    {
        var validator = new CreateCompanyCommandValidator();

        var result = validator.Validate(new CreateCompanyCommand(
            "Test",
            null,
            false,
            true,
            "Asia/Kuala_Lumpur",
            new TimeSpan(8, 30, 0),
            new TimeSpan(17, 30, 0),
            true,
            true,
            true,
            true,
            true,
            true,
            false,
            null));

        Assert.False(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Creates data for the CreateCompanyValidator_Fails_WhenBusinessHoursInvalid operation.
    /// </summary>
    public void CreateCompanyValidator_Fails_WhenBusinessHoursInvalid()
    {
        var validator = new CreateCompanyCommandValidator();

        var result = validator.Validate(new CreateCompanyCommand(
            "Test",
            null,
            false,
            true,
            "Asia/Kuala_Lumpur",
            new TimeSpan(17, 30, 0),
            new TimeSpan(8, 30, 0),
            true,
            true,
            true,
            true,
            true,
            true,
            false,
            Mock.Of<IFormFile>()));

        Assert.False(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Creates data for the CreateCompanyValidator_Fails_WhenNoBusinessDaySelected operation.
    /// </summary>
    public void CreateCompanyValidator_Fails_WhenNoBusinessDaySelected()
    {
        var validator = new CreateCompanyCommandValidator();

        var result = validator.Validate(new CreateCompanyCommand(
            "Test",
            null,
            false,
            true,
            "Asia/Kuala_Lumpur",
            new TimeSpan(8, 30, 0),
            new TimeSpan(17, 30, 0),
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            Mock.Of<IFormFile>()));

        Assert.False(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Creates data for the CreateCompanyValidator_Accepts_FullTzdbTimeZoneId operation.
    /// </summary>
    public void CreateCompanyValidator_Accepts_FullTzdbTimeZoneId()
    {
        var validator = new CreateCompanyCommandValidator();
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        using var logoStream = new MemoryStream(pngBytes);
        IFormFile logo = new FormFile(logoStream, 0, logoStream.Length, "logo", "logo.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        var result = validator.Validate(new CreateCompanyCommand(
            "Test",
            null,
            false,
            true,
            "America/New_York",
            new TimeSpan(8, 30, 0),
            new TimeSpan(17, 30, 0),
            true,
            true,
            true,
            true,
            true,
            true,
            false,
            logo));

        Assert.True(result.IsValid);
    }

    [Fact]
    /// <summary>
    /// Runs the EditCompanyHandler_ReturnsNotFound_WhenCompanyMissing operation.
    /// </summary>
    public async Task EditCompanyHandler_ReturnsNotFound_WhenCompanyMissing()
    {
        using var context = CreateContext();
        var hostEnvironment = CreateHostEnvironment();
        var syncService = new Mock<ICompanyReferenceSyncService>();

        var handler = new UpdateCompanyCommandHandler(context, hostEnvironment.Object, syncService.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateCompanyCommand(
                999,
                "Name",
                null,
                false,
                true,
                "Asia/Kuala_Lumpur",
                new TimeSpan(8, 30, 0),
                new TimeSpan(17, 30, 0),
                true,
                true,
                true,
                true,
                true,
                true,
                false,
                null), CancellationToken.None));
    }

    [Fact]
    /// <summary>
    /// Deletes data for the DeleteCompanyHandler_DeletesCompany_WhenExists operation.
    /// </summary>
    public async Task DeleteCompanyHandler_DeletesCompany_WhenExists()
    {
        using var context = CreateContext();
        context.Companies.Add(new Company { Id = 1, Name = "Delete Me", IsActive = true });
        await context.SaveChangesAsync();

        var hostEnvironment = CreateHostEnvironment();
        var handler = new DeleteCompanyCommandHandler(context, hostEnvironment.Object);

        await handler.Handle(new DeleteCompanyCommand(1), CancellationToken.None);

        Assert.Null(await context.Companies.FindAsync(1));
    }

    [Fact]
    /// <summary>
    /// Runs the SyncCompanyNamesHandler_ReturnsExpectedMessage operation.
    /// </summary>
    public async Task SyncCompanyNamesHandler_ReturnsExpectedMessage()
    {
        var syncService = new Mock<ICompanyReferenceSyncService>();
        syncService
            .Setup(x => x.SyncAllActiveCompaniesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompanyReferenceSyncResult(3, Array.Empty<string>()));

        var handler = new SyncCompanyNamesCommandHandler(syncService.Object);

        var result = await handler.Handle(new SyncCompanyNamesCommand(), CancellationToken.None);

        Assert.Equal("Synchronized 3 company reference record(s).", result.Message);
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

    /// <summary>
    /// Creates data for the CreateHostEnvironment operation.
    /// </summary>
    private static Mock<IWebHostEnvironment> CreateHostEnvironment()
    {
        var host = new Mock<IWebHostEnvironment>();
        host.SetupGet(x => x.WebRootPath).Returns(Path.GetTempPath());
        return host;
    }
}
