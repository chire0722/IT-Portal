using IT_Form.Application.Features.Offboarding.Create;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;

namespace IT_Form.Tests;

public sealed class OffboardingEmployeeDetailsAuthorizationTests
{
    [Fact]
    public async Task EmployeeDetailsQuery_RejectsHrOutsideRequestCompany()
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
        context.OnboardingRequests.Add(new OnboardingRequest
        {
            FullName = "Confidential Employee",
            Branch = "Company B",
            Department = "Finance",
            EmailAddress = "employee@example.test"
        });
        await context.SaveChangesAsync();

        var currentUser = new TestCurrentUserAccessor(
            userName: "hr.user",
            titles: ["HR"],
            companies: ["Company A"]);
        var handler = new GetOffboardingEmployeeDetailsQueryHandler(context, currentUser);

        var result = await handler.Handle(new GetOffboardingEmployeeDetailsQuery(1), CancellationToken.None);

        Assert.Null(result);
    }
}
