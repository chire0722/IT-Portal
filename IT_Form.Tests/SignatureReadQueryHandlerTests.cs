using System.Security.Claims;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Mapping;
using IT_Form.Application.Features.Signature;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using IT_Form.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IT_Form.Tests;

public class SignatureReadQueryHandlerTests
{
    public SignatureReadQueryHandlerTests()
    {
        MapsterConfig.RegisterMappings();
    }

    [Fact]
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
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        var integrity = new Mock<ISignatureIntegrityService>();
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
            EmployeeSignedAt = DateTime.UtcNow.AddHours(-1),
            EmployeeSignerName = "Tina Lee",
            EmployeeSignature = "data:image/png;base64,SIG",
            EmployeeSignatureHmac = "forged"
        });
        await context.SaveChangesAsync();

        var integrity = new Mock<ISignatureIntegrityService>();
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
        Assert.Equal("Digital signature integrity check failed. The document may have been tampered with.", result.Message);
    }

    [Fact]
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
        var integrity = new Mock<ISignatureIntegrityService>();
        var handler = new GetSignaturePdfQueryHandler(context, pdfService.Object, integrity.Object);

        var result = await handler.Handle(new GetSignaturePdfQuery(token, "Onboarding"), CancellationToken.None);

        Assert.Equal(GetSignaturePdfStatus.Expired, result.Status);
        Assert.Equal("This access link has expired (30 day limit). Please contact HR for a new one.", result.Message);
        pdfService.Verify(service => service.GeneratePdf(It.IsAny<OnboardingRequest>()), Times.Never);
    }

    [Fact]
    public async Task GetTransferSignaturePageQuery_ReturnsMappedViewModelForCurrentUser()
    {
        await using var context = CreateContext();

        context.EmployeeTransfers.Add(new EmployeeTransfer
        {
            Id = 11,
            FullName = "Alicia Tan",
            EmployeeID = "E-11",
            FromDepartment = "Operations",
            ToDepartment = "Finance",
            EffectiveDate = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc)
        });
        context.Users.Add(new ApplicationUser
        {
            Id = "u1",
            UserName = "manager@alpha.test",
            FullName = "Manager One",
            Signature = "data:image/png;base64,SAVED"
        });
        await context.SaveChangesAsync();

        var handler = new GetTransferSignaturePageQueryHandler(
            context,
            new TestCurrentUserAccessor("manager@alpha.test", "u1", "Manager"));

        var result = await handler.Handle(new GetTransferSignaturePageQuery(11, "Manager"), CancellationToken.None);

        Assert.Equal(GetTransferSignaturePageStatus.Ready, result.Status);
        var model = Assert.IsType<TransferSignaturePageViewModel>(result.ViewModel);
        Assert.Equal(11, model.Id);
        Assert.Equal("Manager", model.Role);
        Assert.Equal("Alicia Tan", model.EmployeeName);
        Assert.Equal("E-11", model.EmployeeId);
        Assert.Equal("Operations", model.CurrentDepartment);
        Assert.Equal("Finance", model.NewDepartment);
        Assert.Equal("Manager One", model.SignatoryName);
        Assert.Equal("data:image/png;base64,SAVED", model.UserSignature);
    }

    [Fact]
    public async Task GetTransferSignaturePageQuery_ReturnsUnauthorizedWhenCurrentUserMissing()
    {
        await using var context = CreateContext();

        context.EmployeeTransfers.Add(new EmployeeTransfer
        {
            Id = 21,
            FullName = "Una Tan",
            EmployeeID = "E-21",
            FromDepartment = "HR",
            ToDepartment = "IT",
            EffectiveDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc)
        });
        await context.SaveChangesAsync();

        var handler = new GetTransferSignaturePageQueryHandler(
            context,
            new TestCurrentUserAccessor(null, null));

        var result = await handler.Handle(new GetTransferSignaturePageQuery(21, "HR"), CancellationToken.None);

        Assert.Equal(GetTransferSignaturePageStatus.Unauthorized, result.Status);
    }

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

    private sealed class TestCurrentUserAccessor : ICurrentUserAccessor
    {
        public TestCurrentUserAccessor(string? userName, string? userId, params string[] titles)
        {
            var claims = new List<Claim>();

            if (!string.IsNullOrWhiteSpace(userName))
            {
                claims.Add(new Claim(ClaimTypes.Name, userName));
            }

            if (!string.IsNullOrWhiteSpace(userId))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            }

            claims.AddRange(titles.Select(title => new Claim("UserTitle", title)));
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
            UserName = userName;
        }

        public ClaimsPrincipal User { get; }

        public string? UserName { get; }
    }
}
