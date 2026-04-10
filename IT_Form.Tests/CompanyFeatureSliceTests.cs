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

public class CompanyFeatureSliceTests
{
    [Fact]
    public void CreateCompanyValidator_Fails_WhenLogoMissing()
    {
        var validator = new CreateCompanyCommandValidator();

        var result = validator.Validate(new CreateCompanyCommand("Test", null, false, true, null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task EditCompanyHandler_ReturnsNotFound_WhenCompanyMissing()
    {
        using var context = CreateContext();
        var hostEnvironment = CreateHostEnvironment();
        var syncService = new Mock<ICompanyReferenceSyncService>();

        var handler = new UpdateCompanyCommandHandler(context, hostEnvironment.Object, syncService.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateCompanyCommand(999, "Name", null, false, true, null), CancellationToken.None));
    }

    [Fact]
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

    private static Mock<IWebHostEnvironment> CreateHostEnvironment()
    {
        var host = new Mock<IWebHostEnvironment>();
        host.SetupGet(x => x.WebRootPath).Returns(Path.GetTempPath());
        return host;
    }
}
