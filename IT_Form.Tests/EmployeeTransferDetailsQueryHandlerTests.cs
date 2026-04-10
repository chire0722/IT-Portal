using System.Security.Claims;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Mapping;
using IT_Form.Application.Features.EmployeeTransfer.GetEmployeeTransferDetails;
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

public class EmployeeTransferDetailsQueryHandlerTests
{
    public EmployeeTransferDetailsQueryHandlerTests()
    {
        MapsterConfig.RegisterMappings();
    }

    [Fact]
    public async Task Handle_ReturnsReadModelWithSupplementalMetadata()
    {
        await using var context = CreateContext();

        var originalAsset = new ITAsset
        {
            Id = 100,
            AssetTag = "LT-100",
            AssetType = AssetType.Laptop,
            Model = "ThinkPad X1"
        };

        var transferAsset = new ITAsset
        {
            Id = 101,
            AssetTag = "LT-200",
            AssetType = AssetType.Laptop,
            Model = "EliteBook 840"
        };

        var onboarding = new OnboardingRequest
        {
            Id = 1,
            FullName = "Alice Tan",
            Department = "Operations",
            Branch = "Alpha (Lot 1)",
            MailboxAccount = true,
            HasAdobe = true,
            PrecastGroup = true,
            MailboxIT = true
        };

        var transfer = new EmployeeTransfer
        {
            Id = 5,
            OnboardingRequestId = onboarding.Id,
            OnboardingRequest = onboarding,
            FullName = "Alice Tan",
            EmployeeID = "E001",
            FromDepartment = "Operations",
            ToDepartment = "Finance",
            FromBranch = "Alpha (Lot 1)",
            ToBranch = "Alpha (Lot 1)",
            EffectiveDate = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc),
            Status = RequestStatus.PendingITApproval,
            CreatedAt = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc),
            HRSignedAt = new DateTime(2026, 4, 2, 8, 0, 0, DateTimeKind.Utc)
        };

        context.ITAssets.AddRange(originalAsset, transferAsset);
        context.StandardSoftware.AddRange(
            new StandardSoftware
            {
                Id = 1,
                SoftwareName = "Legacy ERP"
            },
            new StandardSoftware
            {
                Id = 2,
                SoftwareName = "Finance Suite"
            });
        context.OnboardingRequests.Add(onboarding);
        context.EmployeeTransfers.Add(transfer);
        context.AddRange(
            new RequestSoftware
            {
                Id = 1,
                OnboardingRequestId = onboarding.Id,
                StandardSoftwareId = 1,
                SoftwareName = "Legacy ERP"
            },
            new RequestEmailGroup
            {
                Id = 1,
                OnboardingRequestId = onboarding.Id,
                EmailGroupId = 1,
                GroupName = "Ops Team",
                GroupEmail = "ops@alpha.test"
            },
            new RequestSharedMailbox
            {
                Id = 1,
                OnboardingRequestId = onboarding.Id,
                SharedMailboxId = 1,
                MailboxName = "ops-shared",
                MailboxAddress = "ops-shared@alpha.test"
            },
            new RequestSoftware
            {
                Id = 2,
                EmployeeTransferId = transfer.Id,
                StandardSoftwareId = 2,
                SoftwareName = "Finance Suite"
            },
            new RequestEmailGroup
            {
                Id = 2,
                EmployeeTransferId = transfer.Id,
                EmailGroupId = 2,
                GroupName = "Finance Team",
                GroupEmail = "finance@alpha.test"
            },
            new RequestSharedMailbox
            {
                Id = 2,
                EmployeeTransferId = transfer.Id,
                SharedMailboxId = 2,
                MailboxName = "finance-shared",
                MailboxAddress = "finance-shared@alpha.test"
            },
            new IssuedAsset
            {
                Id = 3,
                EmployeeTransferId = transfer.Id,
                ITAssetId = transferAsset.Id,
                ITAsset = transferAsset,
                AssetType = AssetType.Laptop
            },
            new FolderPermission
            {
                Id = 4,
                EmployeeTransferId = transfer.Id,
                FolderName = "Finance",
                CanRead = true,
                CanModify = false
            },
            new IssuedAsset
            {
                Id = 6,
                OnboardingRequestId = onboarding.Id,
                ITAssetId = originalAsset.Id,
                ITAsset = originalAsset,
                AssetType = AssetType.Laptop
            });
        context.Companies.Add(new Company
        {
            Id = 7,
            Name = "Alpha",
            Lot = "Lot 1",
            LogoPath = "logos/alpha.png"
        });
        await context.SaveChangesAsync();

        var integrityService = new Mock<ISignatureIntegrityService>();
        var handler = new GetEmployeeTransferDetailsQueryHandler(
            context,
            new TestCurrentUserAccessor("it@alpha.test", "it-1", "IT"),
            integrityService.Object);

        var result = await handler.Handle(new GetEmployeeTransferDetailsQuery(transfer.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var model = Assert.IsType<EmployeeTransferDetailsPageViewModel>(result.Data);
        Assert.Equal(transfer.Id, model.Id);
        Assert.Equal("Alice Tan", model.FullName);
        Assert.Equal("logos/alpha.png", model.CompanyLogoPath);
        Assert.NotNull(model.SlaDeadline);
        Assert.Equal(transfer.HRSignedAt!.Value, model.SlaStartTime);
        Assert.NotNull(model.OnboardingRequest);
        Assert.Single(model.OnboardingRequest!.RequestSoftware);
        Assert.Single(model.CurrentIssuedAssets);
        Assert.Equal("LT-100", model.CurrentIssuedAssets.Single().AssetTag);
        Assert.Single(model.IssuedAssets);
        Assert.True(model.IssuedAssets.Single().HasAssignedAsset);
        Assert.Equal("LT-200", model.IssuedAssets.Single().AssetTag);
        Assert.Single(model.FolderPermissions);
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
