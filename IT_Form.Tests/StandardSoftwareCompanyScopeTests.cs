using IT_Form.Application.Features.StandardSoftware;
using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;

namespace IT_Form.Tests;

public sealed class StandardSoftwareCompanyScopeTests
{
    [Fact]
    public async Task EmployeeLookup_ReturnsOnlyEmployeesInCurrentItCompanyScope()
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
        context.OnboardingRequests.AddRange(
            new OnboardingRequest { FullName = "Alice Company A", Branch = "Company A", Status = RequestStatus.Completed },
            new OnboardingRequest { FullName = "Bob Company B", Branch = "Company B", Status = RequestStatus.Approved });
        await context.SaveChangesAsync();

        var currentUser = new TestCurrentUserAccessor(
            userName: "it.user",
            titles: ["IT"],
            companies: ["Company A"]);
        var handler = new GetStandardSoftwareEmployeesQueryHandler(context, currentUser);

        var result = await handler.Handle(new GetStandardSoftwareEmployeesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var employee = Assert.Single(result.Data!);
        Assert.Equal("Alice Company A", employee.Name);
    }
}
