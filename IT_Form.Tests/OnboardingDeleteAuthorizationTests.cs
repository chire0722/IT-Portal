using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Features.Onboarding.DeleteOnboardingRequest;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;

namespace IT_Form.Tests;

public sealed class OnboardingDeleteAuthorizationTests
{
    [Fact]
    public async Task DeleteHandler_RejectsAuthenticatedUserWithoutHrOrItTitle()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var contextLogger = new Mock<ILogger<ApplicationDbContext>>();
        var encryption = new Mock<ILicenseEncryptionService>();
        encryption.Setup(service => service.EncryptKey(It.IsAny<string>())).Returns((string value) => value);
        encryption.Setup(service => service.DecryptKey(It.IsAny<string>())).Returns((string value) => value);

        await using var context = new ApplicationDbContext(options, httpContextAccessor.Object, contextLogger.Object, encryption.Object);
        context.OnboardingRequests.Add(new OnboardingRequest { FullName = "Test Employee", Branch = "HQ" });
        await context.SaveChangesAsync();

        var currentUser = new TestCurrentUserAccessor("ordinary.user", titles: ["Employee"]);
        var handlerLogger = new Mock<ILogger<DeleteOnboardingRequestCommandHandler>>();
        var handler = new DeleteOnboardingRequestCommandHandler(context, currentUser, handlerLogger.Object);

        var result = await handler.Handle(new DeleteOnboardingRequestCommand(1, false), CancellationToken.None);

        Assert.True(result.IsForbidden);
        Assert.Equal(1, await context.OnboardingRequests.CountAsync());
    }
}
