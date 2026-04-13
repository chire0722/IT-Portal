using System.Security.Claims;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Mapping;
using IT_Form.Application.Features.Offboarding.GetOffboardingDetails;
using IT_Form.Application.Features.Onboarding.GetOnboardingDetails;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using IT_Form.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IT_Form.Tests;

public class RequestDetailsQueryHandlerTests
{
    public RequestDetailsQueryHandlerTests()
    {
        MapsterConfig.RegisterMappings();
    }

    [Fact]
    public async Task OnboardingDetailsHandler_ReturnsReadModelWithMappedCollections()
    {
        await using var context = CreateContext();

        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 10,
            FullName = "Alicia Tan",
            EmployeeID = "E-10",
            Department = "IT",
            Branch = "Alpha (Lot 1)",
            HODEmail = "manager@alpha.test",
            Status = RequestStatus.PendingITApproval,
            RequestDate = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc),
            RequesterSignedAt = new DateTime(2026, 4, 2, 8, 0, 0, DateTimeKind.Utc),
            HasAdobe = true,
            MailboxAccount = true
        });
        context.StandardSoftware.Add(new StandardSoftware
        {
            Id = 7,
            SoftwareName = "Adobe Acrobat"
        });
        context.SoftwareLicenses.Add(new SoftwareLicense
        {
            Id = 70,
            SoftwareName = "Adobe Acrobat",
            StandardSoftwareId = 7,
            TotalLicenses = 1,
            ContractLimit = 1
        });
        context.AddRange(
            new RequestEmailGroup
            {
                Id = 1,
                OnboardingRequestId = 10,
                EmailGroupId = 5,
                GroupName = "IT Team",
                GroupEmail = "it@alpha.test"
            },
            new RequestSharedMailbox
            {
                Id = 2,
                OnboardingRequestId = 10,
                SharedMailboxId = 6,
                MailboxName = "helpdesk",
                MailboxAddress = "helpdesk@alpha.test"
            },
            new RequestSoftware
            {
                Id = 3,
                OnboardingRequestId = 10,
                StandardSoftwareId = 7,
                SoftwareName = "Adobe Acrobat"
            },
            new IssuedAsset
            {
                Id = 4,
                OnboardingRequestId = 10,
                AssetType = AssetType.Laptop,
                AssetTag = "LT-10",
                BrandModel = "ThinkPad X1",
                SerialNumber = "SN-10"
            },
            new IssuedSoftware
            {
                Id = 5,
                OnboardingRequestId = 10,
                SoftwareLicenseId = 70,
                SoftwareName = "Adobe Acrobat",
                Notes = "Assigned by IT"
            },
            new FolderPermission
            {
                Id = 6,
                OnboardingRequestId = 10,
                FolderName = "IT Shared",
                CanRead = true,
                CanModify = true
            });
        context.Companies.Add(new Company
        {
            Id = 20,
            Name = "Alpha",
            Lot = "Lot 1",
            LogoPath = "logos/alpha.png",
            BusinessStartTime = new TimeSpan(9, 30, 0),
            BusinessEndTime = new TimeSpan(18, 30, 0),
            WorksOnSaturday = false
        });
        await context.SaveChangesAsync();

        var handler = new GetOnboardingDetailsQueryHandler(
            context,
            CreateUserManager("signature://hr"),
            Mock.Of<ISignatureIntegrityService>(),
            new TestCurrentUserAccessor("it@alpha.test", "it-1", "IT"));

        var result = await handler.Handle(new GetOnboardingDetailsQuery(10), CancellationToken.None);

        Assert.Equal(GetOnboardingDetailsStatus.Ready, result.Status);
        var model = Assert.IsType<OnboardingDetailsPageViewModel>(result.ViewModel);
        Assert.Equal("Alicia Tan", model.FullName);
        Assert.Equal("signature://hr", model.CurrentUserSignature);
        Assert.Equal("logos/alpha.png", model.CompanyLogoPath);
        Assert.Equal("Asia/Kuala_Lumpur", model.CompanyTimeZoneId);
        Assert.NotNull(model.SlaDeadline);
        Assert.Equal(
            SlaHelper.CalculateSlaDeadline(
                new DateTime(2026, 4, 2, 8, 0, 0, DateTimeKind.Utc),
                3,
                new TimeSpan(9, 30, 0),
                new TimeSpan(18, 30, 0),
                [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday]),
            model.SlaDeadline!.Value);
        Assert.Single(model.IssuedAssets);
        Assert.Equal("LT-10", model.IssuedAssets[0].AssetTag);
        Assert.Single(model.IssuedSoftware);
        Assert.Equal("Adobe Acrobat", model.IssuedSoftware[0].SoftwareName);
        Assert.Single(model.RequestSoftware);
        Assert.Equal(7, model.RequestSoftware[0].Id);
        Assert.Single(model.FolderPermissions);
    }

    [Fact]
    public async Task OffboardingDetailsHandler_ReturnsReadModelWithOnboardingSnapshot()
    {
        await using var context = CreateContext();

        var linkedAsset = new ITAsset
        {
            Id = 100,
            AssetTag = "LT-77",
            AssetType = AssetType.Laptop,
            Model = "EliteBook",
            SerialNumber = "SER-77",
            Status = AssetStatus.Available
        };

        var onboarding = new OnboardingRequest
        {
            Id = 30,
            FullName = "Brian Lee",
            EmailAddress = "brian@alpha.test",
            MailboxAccount = true,
            HasAdobe = true,
            EmployeeSignature = "signature://employee",
            EmployeeSignerName = "Brian Lee",
            EmployeeSignedAt = new DateTime(2026, 3, 30, 8, 0, 0, DateTimeKind.Utc)
        };

        context.ITAssets.Add(linkedAsset);
        context.StandardSoftware.Add(new StandardSoftware
        {
            Id = 8,
            SoftwareName = "Finance Suite"
        });
        context.SoftwareLicenses.Add(new SoftwareLicense
        {
            Id = 80,
            SoftwareName = "Finance Suite",
            StandardSoftwareId = 8,
            TotalLicenses = 1,
            ContractLimit = 1
        });
        context.OnboardingRequests.Add(onboarding);
        context.AddRange(
            new FolderPermission
            {
                Id = 31,
                OnboardingRequestId = 30,
                FolderName = "Finance Shared",
                CanRead = true,
                CanModify = false
            },
            new IssuedAsset
            {
                Id = 32,
                OnboardingRequestId = 30,
                ITAsset = linkedAsset,
                ITAssetId = linkedAsset.Id,
                AssetType = AssetType.Laptop
            },
            new IssuedSoftware
            {
                Id = 33,
                OnboardingRequestId = 30,
                SoftwareLicenseId = 80,
                SoftwareName = "Finance Suite"
            });
        context.OffboardingRequests.Add(new OffboardingRequest
        {
            Id = 40,
            OnboardingRequestId = onboarding.Id,
            OnboardingRequest = onboarding,
            FullName = "Brian Lee",
            EmployeeID = "EMP-40",
            Department = "Finance",
            Branch = "Alpha (Lot 1)",
            HODEmail = "manager@alpha.test",
            Status = RequestStatus.PendingHODApproval,
            LastDayOfWork = new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc),
            RequestDate = new DateTime(2026, 4, 10, 8, 0, 0, DateTimeKind.Utc),
            HRSignedAt = new DateTime(2026, 4, 11, 8, 0, 0, DateTimeKind.Utc)
        });
        context.Users.Add(new ApplicationUser
        {
            Id = "manager-1",
            Email = "manager@alpha.test",
            FullName = "Manager Alpha",
            UserName = "manager.alpha",
            IsActive = true
        });
        context.Companies.Add(new Company
        {
            Id = 41,
            Name = "Alpha",
            Lot = "Lot 1",
            LogoPath = "logos/alpha.png",
            BusinessStartTime = new TimeSpan(9, 30, 0),
            BusinessEndTime = new TimeSpan(18, 30, 0),
            WorksOnSaturday = false
        });
        await context.SaveChangesAsync();

        var handler = new GetOffboardingDetailsQueryHandler(
            context,
            CreateUserManager("signature://it"),
            Mock.Of<ISignatureIntegrityService>(),
            new TestCurrentUserAccessor("it@alpha.test", "it-1", "IT"));

        var result = await handler.Handle(new GetOffboardingDetailsQuery(40), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var model = Assert.IsType<OffboardingDetailsPageViewModel>(result.Data);
        Assert.Equal("Manager Alpha", model.ManagerDisplayName);
        Assert.Equal("signature://it", model.CurrentUserSignature);
        Assert.Equal("logos/alpha.png", model.CompanyLogoPath);
        Assert.Equal("Asia/Kuala_Lumpur", model.CompanyTimeZoneId);
        Assert.Equal(
            SlaHelper.CalculateSlaDeadline(
                new DateTime(2026, 4, 11, 8, 0, 0, DateTimeKind.Utc),
                3,
                new TimeSpan(9, 30, 0),
                new TimeSpan(18, 30, 0),
                [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday]),
            model.SlaDeadline);
        Assert.NotNull(model.OnboardingRequest);
        Assert.Single(model.OnboardingRequest!.IssuedAssets);
        Assert.True(model.OnboardingRequest.IssuedAssets[0].IsReturned);
        Assert.Equal(AssetStatus.Available, model.OnboardingRequest.IssuedAssets[0].ITAsset!.Status);
        Assert.Single(model.OnboardingRequest.IssuedSoftware);
        Assert.Equal("Finance Suite", model.OnboardingRequest.IssuedSoftware[0].SoftwareName);
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

    private static UserManager<ApplicationUser> CreateUserManager(string signature)
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var manager = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        manager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(new ApplicationUser
            {
                Id = "current-user",
                Signature = signature
            });
        return manager.Object;
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
