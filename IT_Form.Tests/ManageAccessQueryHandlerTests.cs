using System.Security.Claims;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Mapping;
using IT_Form.Application.Features.Onboarding.GetManageAccess;
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

public class ManageAccessQueryHandlerTests
{
    public ManageAccessQueryHandlerTests()
    {
        MapsterConfig.RegisterMappings();
    }

    [Fact]
    public async Task Handle_ReturnsReadModelUsingCurrentEmployeeContext()
    {
        await using var context = CreateContext();

        context.Companies.Add(new Company
        {
            Id = 1,
            Name = "Alpha",
            Lot = "Lot 1",
            IsActive = true
        });

        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 10,
            FullName = "Alicia Tan",
            EmployeeID = "E-10",
            Designation = "Executive",
            Department = "Operations",
            Branch = "Alpha (Lot 1)",
            JoinDate = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            Status = RequestStatus.Completed,
            RequestEmailGroups =
            [
                new RequestEmailGroup
                {
                    Id = 100,
                    EmailGroupId = 11,
                    GroupName = "Operations Team",
                    GroupEmail = "ops@alpha.test"
                }
            ],
            RequestSharedMailboxes =
            [
                new RequestSharedMailbox
                {
                    Id = 101,
                    SharedMailboxId = 21,
                    MailboxName = "ops-shared",
                    MailboxAddress = "ops-shared@alpha.test"
                }
            ],
            FolderPermissions =
            [
                new FolderPermission
                {
                    Id = 102,
                    FolderName = "Operations",
                    CanRead = true,
                    CanModify = false
                }
            ]
        });

        context.EmployeeTransfers.Add(new EmployeeTransfer
        {
            Id = 30,
            OnboardingRequestId = 10,
            FullName = "Alicia Tan",
            FromDepartment = "Operations",
            ToDepartment = "Finance",
            FromBranch = "Alpha (Lot 1)",
            ToBranch = "Alpha (Lot 1)",
            Status = RequestStatus.Completed,
            CreatedAt = new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc),
            CompletedAt = new DateTime(2026, 3, 5, 8, 0, 0, DateTimeKind.Utc)
        });

        context.EmailGroups.AddRange(
            new EmailGroup
            {
                Id = 11,
                GroupName = "Operations Team",
                GroupEmail = "ops@alpha.test",
                IsActive = true
            },
            new EmailGroup
            {
                Id = 12,
                GroupName = "Finance Team",
                GroupEmail = "finance@alpha.test",
                IsActive = true
            });

        context.SharedMailboxes.AddRange(
            new SharedMailbox
            {
                Id = 21,
                MailboxName = "ops-shared",
                MailboxAddress = "ops-shared@alpha.test",
                IsActive = true
            },
            new SharedMailbox
            {
                Id = 22,
                MailboxName = "finance-shared",
                MailboxAddress = "finance-shared@alpha.test",
                IsActive = true
            });

        context.ServerFolders.AddRange(
            new ServerFolder { Id = 31, FolderName = "Operations", IsActive = true },
            new ServerFolder { Id = 32, FolderName = "Finance", IsActive = true });

        await context.SaveChangesAsync();

        var handler = new GetManageAccessQueryHandler(
            new CurrentEmployeeAccessService(context),
            new TestCurrentUserAccessor("it@alpha.test", "it-1", "IT"));

        var model = await handler.Handle(new GetManageAccessQuery(10), CancellationToken.None);

        Assert.NotNull(model);
        Assert.Equal(10, model!.OnboardingId);
        Assert.Equal("Alicia Tan", model.FullName);
        Assert.Equal("Finance", model.CurrentDepartment);
        Assert.Equal("Alpha (Lot 1)", model.CurrentBranch);
        Assert.Equal(new DateTime(2026, 3, 5, 8, 0, 0, DateTimeKind.Utc), model.LastTransferCompletedAt);
        Assert.Equal([11], model.SelectedEmailGroupIds);
        Assert.Equal([21], model.SelectedSharedMailboxIds);
        Assert.Equal(2, model.AvailableEmailGroups.Count);
        Assert.Equal("Finance Team", model.AvailableEmailGroups[0].GroupName);
        Assert.IsType<ManageAccessEmailGroupOptionViewModel>(model.AvailableEmailGroups[0]);
        Assert.Equal(2, model.AvailableSharedMailboxes.Count);
        Assert.IsType<ManageAccessSharedMailboxOptionViewModel>(model.AvailableSharedMailboxes[0]);
        Assert.Contains(model.FolderPermissions, folder => folder.FolderName == "Operations" && folder.CanRead);
        Assert.Contains(model.FolderPermissions, folder => folder.FolderName == "Finance");
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
        public TestCurrentUserAccessor(string userName, string userId, params string[] titles)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, userName),
                new(ClaimTypes.NameIdentifier, userId)
            };

            claims.AddRange(titles.Select(title => new Claim("UserTitle", title)));
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
            UserName = userName;
        }

        public ClaimsPrincipal User { get; }

        public string? UserName { get; }
    }
}
