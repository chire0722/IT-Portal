using IT_Form.Application.Features.Offboarding.ActionWrites;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;

namespace IT_Form.Tests;

public sealed class OffboardingCheckerAuthorizationTests
{
    [Fact]
    public async Task CheckerApproval_RejectsHodOutsideRequestCompanyBeforeMutation()
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
        context.OffboardingRequests.Add(new OffboardingRequest
        {
            FullName = "Test Employee",
            Branch = "Company B",
            CreatedByUserId = "maker.user",
            AssignedHodId = "another-hod-id",
            Status = RequestStatus.PendingApproval
        });
        await context.SaveChangesAsync();

        var currentUser = new TestCurrentUserAccessor(
            userName: "other.hod",
            userId: "other-hod-id",
            titles: ["HOD"],
            companies: ["Company A"]);
        var sender = new Mock<ISender>();
        var handler = new ProcessOffboardingCheckerApprovalCommandHandler(
            context,
            currentUser,
            null!,
            sender.Object);

        var result = await handler.Handle(
            new ProcessOffboardingCheckerApprovalCommand(1, "signature-data"),
            CancellationToken.None);

        Assert.True(result.IsForbidden);
        var persisted = await context.OffboardingRequests.SingleAsync();
        Assert.Equal(RequestStatus.PendingApproval, persisted.Status);
        Assert.Null(persisted.ApprovedByUserId);
    }
}
