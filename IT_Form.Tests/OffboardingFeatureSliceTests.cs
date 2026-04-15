using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Features.Offboarding.Step2;
using IT_Form.Application.Features.Offboarding.RegenerateOffboardingToken;
using IT_Form.Application.Features.Offboarding.RejectOffboardingRequest;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IT_Form.Tests;

public class OffboardingFeatureSliceTests
{
    [Fact]
    public void RejectValidator_Fails_WhenReasonMissing()
    {
        var validator = new RejectOffboardingRequestCommandValidator();

        var result = validator.Validate(new RejectOffboardingRequestCommand(1, string.Empty));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task RejectHandler_ReturnsRejected_WhenUserIsITAndRequestEditable()
    {
        using var context = CreateContext();
        context.OffboardingRequests.Add(new OffboardingRequest
        {
            Id = 11,
            Status = RequestStatus.PendingITApproval,
            FullName = "Emp A",
            LastDayOfWork = DateTime.Today,
            Branch = "HQ",
            HODEmail = "hod@example.com"
        });
        await context.SaveChangesAsync();

        var currentUser = new Mock<ICurrentUserAccessor>();
        currentUser.SetupGet(x => x.User).Returns(BuildUser("it-user", "IT", "HQ"));
        currentUser.SetupGet(x => x.UserName).Returns("it-user");

        var userManager = CreateUserManager();
        var handler = new RejectOffboardingRequestCommandHandler(context, currentUser.Object, userManager.Object);

        var result = await handler.Handle(new RejectOffboardingRequestCommand(11, "Not eligible"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Request has been rejected.", result.Message);
        Assert.Equal(11, result.RequestId);
    }

    [Fact]
    public void RegenerateTokenValidator_Fails_WhenIdInvalid()
    {
        var validator = new RegenerateOffboardingTokenCommandValidator();

        var result = validator.Validate(new RegenerateOffboardingTokenCommand(0));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task RegenerateTokenHandler_Regenerates_WhenPendingEmployeeSign()
    {
        using var context = CreateContext();
        var oldToken = Guid.NewGuid();
        context.OffboardingRequests.Add(new OffboardingRequest
        {
            Id = 12,
            Status = RequestStatus.PendingEmployeeSign,
            AccessToken = oldToken,
            FullName = "Emp B",
            LastDayOfWork = DateTime.Today,
            Branch = "HQ",
            HODEmail = "hod@example.com"
        });
        await context.SaveChangesAsync();

        var currentUser = new Mock<ICurrentUserAccessor>();
        currentUser.SetupGet(x => x.User).Returns(BuildUser("hr-user", "HR", "HQ"));

        var handler = new RegenerateOffboardingTokenCommandHandler(context, currentUser.Object);
        var result = await handler.Handle(new RegenerateOffboardingTokenCommand(12), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Signature link has been regenerated. The old link is now invalid.", result.Message);
        Assert.Equal(12, result.RequestId);

        var saved = await context.OffboardingRequests.FindAsync(12);
        Assert.NotNull(saved);
        Assert.NotEqual(oldToken, saved!.AccessToken);
    }

    [Fact]
    public async Task GetStep2Handler_ReturnsForbidden_WhenItUserTargetsDifferentCompany()
    {
        using var context = CreateContext();
        context.OffboardingRequests.Add(new OffboardingRequest
        {
            Id = 13,
            Status = RequestStatus.PendingITApproval,
            FullName = "Emp C",
            LastDayOfWork = DateTime.Today,
            Branch = "Beta",
            HODEmail = "hod@example.com"
        });
        await context.SaveChangesAsync();

        var handler = new GetOffboardingStep2QueryHandler(
            context,
            new TestCurrentUserAccessor("it-user", titles: ["IT"], companies: ["Alpha"]),
            CreateUserManager().Object);

        var result = await handler.Handle(new GetOffboardingStep2Query(13), CancellationToken.None);

        Assert.True(result.IsForbidden);
        Assert.False(result.IsSuccess);
    }

    private static ClaimsPrincipal BuildUser(string username, string title, string? company = null)
    {
        var identity = new ClaimsIdentity("mock");
        identity.AddClaim(new Claim(ClaimTypes.Name, username));
        identity.AddClaim(new Claim("UserTitle", title));
        if (!string.IsNullOrWhiteSpace(company))
        {
            identity.AddClaim(new Claim("UserCompany", company));
        }
        return new ClaimsPrincipal(identity);
    }

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
