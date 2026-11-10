// Project comment: SignatureReadQueryHandlerTests.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Mapping;
using IT_Form.Application.Features.Signature;
using IT_Form.Application.Features.Signature.AcknowledgeEmployeeSignature;
using IT_Form.Data;
using IT_Form.Infrastructure.Notifications;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using IT_Form.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IT_Form.Tests;

/// <summary>
/// Defines the SignatureReadQueryHandlerTests class used by the IT Form application.
/// </summary>
public class SignatureReadQueryHandlerTests
{
    public SignatureReadQueryHandlerTests()
    {
        MapsterConfig.RegisterMappings();
    }

    [Fact]
    /// <summary>
    /// Gets the data required by the GetSignatureAcknowledgePageQuery_ReturnsTypedOnboardingViewModel operation.
    /// </summary>
    public async Task GetSignatureAcknowledgePageQuery_ReturnsTypedOnboardingViewModel()
    {
        await using var context = CreateContext();
        var token = Guid.NewGuid();

        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 1,
            AccessToken = token,
            FullName = "Alicia Tan",
            Department = "Finance",
            Branch = "Alpha HQ",
            Status = RequestStatus.PendingEmployeeSign,
            RequesterSignerName = "HR User",
            RequesterSignedAt = DateTime.UtcNow.AddDays(-2),
            RequesterSignature = "data:image/png;base64,REQ",
            RequesterSignatureHmac = "HMAC",
            ITAdminSignerName = "IT User",
            ITAdminSignedAt = DateTime.UtcNow.AddDays(-1),
            ITAdminSignature = "data:image/png;base64,IT",
            ITAdminSignatureHmac = "HMAC",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        var integrity = CreateValidIntegrityMock();
        var handler = new GetSignatureAcknowledgePageQueryHandler(context, integrity.Object);

        var result = await handler.Handle(new GetSignatureAcknowledgePageQuery(token, "Onboarding"), CancellationToken.None);

        Assert.Equal(GetSignatureAcknowledgePageStatus.Ready, result.Status);
        var model = Assert.IsType<SignatureAcknowledgePageViewModel>(result.ViewModel);
        Assert.Equal("Onboarding", model.RequestType);
        Assert.Equal(token, model.Token);
        Assert.Equal("Alicia Tan", model.FullName);
        Assert.Equal("Finance", model.Department);
        Assert.Equal("Alpha HQ", model.Company);
        Assert.False(model.AlreadySigned);
        Assert.False(model.Expired);
    }

    [Fact]
    /// <summary>
    /// Gets the data required by the GetSignatureAcknowledgePageQuery_UsesRegeneratedTimestamp_WhenCompletedAtIsOld operation.
    /// </summary>
    public async Task GetSignatureAcknowledgePageQuery_UsesRegeneratedTimestamp_WhenCompletedAtIsOld()
    {
        await using var context = CreateContext();
        var token = Guid.NewGuid();
        var now = DateTime.UtcNow;

        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 2,
            AccessToken = token,
            FullName = "Regenerated User",
            Department = "IT",
            Branch = "HQ",
            Status = RequestStatus.PendingEmployeeSign,
            RequesterSignerName = "HR User",
            RequesterSignedAt = DateTime.UtcNow.AddDays(-2),
            RequesterSignature = "data:image/png;base64,REQ",
            RequesterSignatureHmac = "HMAC",
            ITAdminSignerName = "IT User",
            ITAdminSignedAt = DateTime.UtcNow.AddDays(-1),
            ITAdminSignature = "data:image/png;base64,IT",
            ITAdminSignatureHmac = "HMAC",
            CreatedAt = now.AddDays(-20),
            CompletedAt = now.AddDays(-10),
            UpdatedAt = now.AddMinutes(-5)
        });
        await context.SaveChangesAsync();

        var integrity = CreateValidIntegrityMock();
        var handler = new GetSignatureAcknowledgePageQueryHandler(context, integrity.Object);

        var result = await handler.Handle(new GetSignatureAcknowledgePageQuery(token, "Onboarding"), CancellationToken.None);

        Assert.Equal(GetSignatureAcknowledgePageStatus.Ready, result.Status);
        var model = Assert.IsType<SignatureAcknowledgePageViewModel>(result.ViewModel);
        Assert.False(model.Expired);
    }

    [Fact]
    /// <summary>
    /// Runs the AcknowledgeEmployeeSignatureCommand_UsesRegeneratedTimestamp_WhenCompletedAtIsOld operation.
    /// </summary>
    public async Task AcknowledgeEmployeeSignatureCommand_UsesRegeneratedTimestamp_WhenCompletedAtIsOld()
    {
        await using var context = CreateContext();
        var token = Guid.NewGuid();
        var now = DateTime.UtcNow;
        const string signatureData = "data:image/png;base64,iVBORw0KGgo=";

        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 3,
            AccessToken = token,
            FullName = "Regenerated Signer",
            Department = "IT",
            Branch = "HQ",
            Status = RequestStatus.PendingEmployeeSign,
            RequesterSignerName = "HR User",
            RequesterSignedAt = DateTime.UtcNow.AddDays(-2),
            RequesterSignature = "data:image/png;base64,REQ",
            RequesterSignatureHmac = "HMAC",
            ITAdminSignerName = "IT User",
            ITAdminSignedAt = DateTime.UtcNow.AddDays(-1),
            ITAdminSignature = "data:image/png;base64,IT",
            ITAdminSignatureHmac = "HMAC",
            CreatedAt = now.AddDays(-20),
            CompletedAt = now.AddDays(-10),
            UpdatedAt = now.AddMinutes(-5)
        });
        await context.SaveChangesAsync();

        var integrity = CreateValidIntegrityMock();
        integrity
            .Setup(service => service.ComputeHmac(
                "OnboardingRequest",
                "3",
                "Regenerated Signer",
                It.IsAny<DateTime>(),
                signatureData))
            .Returns("HMAC");

        var handler = CreateAcknowledgeHandler(context, integrity.Object);

        var result = await handler.Handle(
            new AcknowledgeEmployeeSignatureCommand(token, "Onboarding", signatureData, PdpaConsent: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Acknowledgement completed successfully.", result.Message);

        var saved = await context.OnboardingRequests.SingleAsync(r => r.Id == 3);
        Assert.Equal(RequestStatus.Completed, saved.Status);
        Assert.Equal(signatureData, saved.EmployeeSignature);
        Assert.Equal("HMAC", saved.EmployeeSignatureHmac);
        Assert.True(saved.IsPdpaConsented);
    }

    [Fact]
    /// <summary>
    /// Gets the data required by the GetSignatureAcknowledgePageQuery_ReturnsBadRequestWhenIntegrityFails operation.
    /// </summary>
    public async Task GetSignatureAcknowledgePageQuery_ReturnsBadRequestWhenIntegrityFails()
    {
        await using var context = CreateContext();
        var token = Guid.NewGuid();

        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 5,
            AccessToken = token,
            FullName = "Tina Lee",
            Department = "IT",
            Branch = "HQ",
            Status = RequestStatus.Completed,
            RequesterSignerName = "HR User",
            RequesterSignedAt = DateTime.UtcNow.AddDays(-2),
            RequesterSignature = "data:image/png;base64,REQ",
            RequesterSignatureHmac = "HMAC",
            ITAdminSignerName = "IT User",
            ITAdminSignedAt = DateTime.UtcNow.AddDays(-1),
            ITAdminSignature = "data:image/png;base64,IT",
            ITAdminSignatureHmac = "HMAC",
            EmployeeSignedAt = DateTime.UtcNow.AddHours(-1),
            EmployeeSignerName = "Tina Lee",
            EmployeeSignature = "data:image/png;base64,SIG",
            EmployeeSignatureHmac = "forged"
        });
        await context.SaveChangesAsync();

        var integrity = CreateValidIntegrityMock();
        integrity
            .Setup(service => service.Verify(
                "OnboardingRequest",
                "5",
                "Tina Lee",
                It.IsAny<DateTime>(),
                "data:image/png;base64,SIG",
                "forged"))
            .Returns(false);

        var handler = new GetSignatureAcknowledgePageQueryHandler(context, integrity.Object);

        var result = await handler.Handle(new GetSignatureAcknowledgePageQuery(token, "Onboarding"), CancellationToken.None);

        Assert.Equal(GetSignatureAcknowledgePageStatus.IntegrityFailed, result.Status);
        Assert.Equal("A previous signature is missing integrity metadata or cannot be trusted. Contact HR to review the record.", result.Message);
    }

    [Fact]
    /// <summary>
    /// Gets the data required by the GetSignaturePdfQuery_ReturnsForbiddenObjectWhenOnboardingLinkExpired operation.
    /// </summary>
    public async Task GetSignaturePdfQuery_ReturnsForbiddenObjectWhenOnboardingLinkExpired()
    {
        await using var context = CreateContext();
        var token = Guid.NewGuid();

        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 7,
            AccessToken = token,
            FullName = "Expired User",
            CreatedAt = DateTime.UtcNow.AddDays(-40)
        });
        await context.SaveChangesAsync();

        var pdfService = new Mock<IPdfService>();
        var integrity = CreateValidIntegrityMock();
        var handler = new GetSignaturePdfQueryHandler(context, pdfService.Object, integrity.Object);

        var result = await handler.Handle(new GetSignaturePdfQuery(token, "Onboarding"), CancellationToken.None);

        Assert.Equal(GetSignaturePdfStatus.Expired, result.Status);
        Assert.Equal("This access link has expired (30 day limit). Please contact HR for a new one.", result.Message);
        pdfService.Verify(service => service.GeneratePdf(It.IsAny<OnboardingRequest>()), Times.Never);
    }

    [Fact]
    /// <summary>
    /// Gets the data required by the GetSignaturePdfQuery_UsesRegeneratedTimestamp_WhenCompletedAtIsOld operation.
    /// </summary>
    public async Task GetSignaturePdfQuery_UsesRegeneratedTimestamp_WhenCompletedAtIsOld()
    {
        await using var context = CreateContext();
        var token = Guid.NewGuid();
        var now = DateTime.UtcNow;

        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 8,
            AccessToken = token,
            FullName = "../../evil\r\nname",
            Status = RequestStatus.PendingEmployeeSign,
            RequesterSignerName = "HR User",
            RequesterSignedAt = DateTime.UtcNow.AddDays(-2),
            RequesterSignature = "data:image/png;base64,REQ",
            RequesterSignatureHmac = "HMAC",
            ITAdminSignerName = "IT User",
            ITAdminSignedAt = DateTime.UtcNow.AddDays(-1),
            ITAdminSignature = "data:image/png;base64,IT",
            ITAdminSignatureHmac = "HMAC",
            CreatedAt = now.AddDays(-60),
            CompletedAt = now.AddDays(-40),
            UpdatedAt = now.AddMinutes(-5)
        });
        await context.SaveChangesAsync();

        var pdfService = new Mock<IPdfService>();
        pdfService
            .Setup(service => service.GeneratePdf(It.IsAny<OnboardingRequest>()))
            .Returns(new byte[] { 1, 2, 3 });
        var integrity = CreateValidIntegrityMock();
        var handler = new GetSignaturePdfQueryHandler(context, pdfService.Object, integrity.Object);

        var result = await handler.Handle(new GetSignaturePdfQuery(token, "Onboarding"), CancellationToken.None);

        Assert.Equal(GetSignaturePdfStatus.Ready, result.Status);
        Assert.NotNull(result.File);
        Assert.Equal("Onboarding_evil_name.pdf", result.File!.FileDownloadName);
        Assert.DoesNotContain("/", result.File.FileDownloadName);
        Assert.DoesNotContain("\\", result.File.FileDownloadName);
        Assert.DoesNotContain("\r", result.File.FileDownloadName);
        Assert.DoesNotContain("\n", result.File.FileDownloadName);
        pdfService.Verify(service => service.GeneratePdf(It.IsAny<OnboardingRequest>()), Times.Once);
    }

    private static Mock<ISignatureIntegrityService> CreateValidIntegrityMock()
    {
        var integrity = new Mock<ISignatureIntegrityService>();
        integrity
            .Setup(service => service.Verify(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(true);
        return integrity;
    }

    /// <summary>
    /// Creates data for the CreateAcknowledgeHandler operation.
    /// </summary>
    private static AcknowledgeEmployeeSignatureCommandHandler CreateAcknowledgeHandler(
        ApplicationDbContext context,
        ISignatureIntegrityService integrity)
    {
        var queue = new Mock<IEmailBackgroundQueue>();
        queue
            .Setup(service => service.QueueEmailAsync(
                It.IsAny<IEmailBackgroundMessage>(),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        return new AcknowledgeEmployeeSignatureCommandHandler(
            context,
            Mock.Of<ILogger<AcknowledgeEmployeeSignatureCommandHandler>>(),
            integrity,
            queue.Object,
            CreateUserManager().Object,
            new ConfigurationBuilder().Build(),
            Mock.Of<IHttpContextAccessor>());
    }

    /// <summary>
    /// Creates data for the CreateUserManager operation.
    /// </summary>
    private static Mock<UserManager<ApplicationUser>> CreateUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }

    /// <summary>
    /// Creates data for the CreateContext operation.
    /// </summary>
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var logger = new Mock<ILogger<ApplicationDbContext>>();
        var encryption = new Mock<ILicenseEncryptionService>();
        encryption.Setup(service => service.EncryptKey(It.IsAny<string>())).Returns((string value) => value);
        encryption.Setup(service => service.DecryptKey(It.IsAny<string>())).Returns((string value) => value);

        return new ApplicationDbContext(options, httpContextAccessor.Object, logger.Object, encryption.Object);
    }
}
