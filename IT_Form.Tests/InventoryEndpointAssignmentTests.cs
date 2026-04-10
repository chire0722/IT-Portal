using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Tests;

public sealed class InventoryEndpointAssignmentTests : ControllerTestBase
{
    [Fact]
    public async Task ResolveVerificationAssignmentAsync_MatchesCurrentEmployee_ByEmail()
    {
        using var context = CreateContext();

        context.Users.Add(new ApplicationUser
        {
            Id = "user-1",
            UserName = "alice",
            Email = "alice@test.local",
            FullName = "Alice Tan"
        });
        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 11,
            FullName = "Alice Tan",
            EmailAddress = "alice@test.local",
            Department = "IT",
            Status = RequestStatus.Completed
        });
        await context.SaveChangesAsync();

        var assignment = await InventoryEndpoints.ResolveVerificationAssignmentAsync(context, "user-1");

        Assert.NotNull(assignment);
        Assert.Equal(11, assignment!.OnboardingRequestId);
        Assert.Null(assignment.CustomEmployeeName);
    }

    [Fact]
    public async Task ResolveVerificationAssignmentAsync_FallsBackToManualName_WhenCurrentEmployeeRecordMissing()
    {
        using var context = CreateContext();

        context.Users.Add(new ApplicationUser
        {
            Id = "user-2",
            UserName = "manual-user",
            Email = "manual@test.local",
            FullName = "Manual User"
        });
        await context.SaveChangesAsync();

        var assignment = await InventoryEndpoints.ResolveVerificationAssignmentAsync(context, "user-2");

        Assert.NotNull(assignment);
        Assert.Null(assignment!.OnboardingRequestId);
        Assert.Equal("Manual User", assignment.CustomEmployeeName);
    }

    [Fact]
    public async Task ApplyVerificationAssignmentAsync_AssignsAssetToSelectedEmployee()
    {
        using var context = CreateContext();

        context.Users.Add(new ApplicationUser
        {
            Id = "user-3",
            UserName = "brian",
            Email = "brian@test.local",
            FullName = "Brian Lee"
        });
        context.OnboardingRequests.Add(new OnboardingRequest
        {
            Id = 12,
            FullName = "Brian Lee",
            EmailAddress = "brian@test.local",
            Department = "Finance",
            Status = RequestStatus.Approved
        });

        var asset = new ITAsset
        {
            AssetTag = "AUTO-001",
            SerialNumber = "SN-001",
            AssetType = AssetType.Laptop,
            Status = AssetStatus.Available,
            Model = "Dell Latitude"
        };
        context.ITAssets.Add(asset);
        await context.SaveChangesAsync();

        var token = new DeviceVerificationToken
        {
            Token = Guid.NewGuid().ToString("N"),
            EmployeeId = "user-3",
            IsVerified = false
        };

        await InventoryEndpoints.ApplyVerificationAssignmentAsync(context, asset, token);
        await context.SaveChangesAsync();

        var updatedAsset = await context.ITAssets.FirstAsync(item => item.Id == asset.Id);
        var issue = await context.IssuedAssets
            .OrderByDescending(item => item.Id)
            .FirstOrDefaultAsync(item => item.ITAssetId == asset.Id);

        Assert.Equal(AssetStatus.Issued, updatedAsset.Status);
        Assert.NotNull(issue);
        Assert.Equal(12, issue!.OnboardingRequestId);
        Assert.Null(issue.CustomEmployeeName);
    }

    [Fact]
    public async Task ApplyVerificationAssignmentAsync_DoesNothing_WhenTokenHasNoEmployee()
    {
        using var context = CreateContext();

        var asset = new ITAsset
        {
            AssetTag = "AUTO-002",
            SerialNumber = "SN-002",
            AssetType = AssetType.DesktopPC,
            Status = AssetStatus.Available,
            Model = "HP ProDesk"
        };
        context.ITAssets.Add(asset);
        await context.SaveChangesAsync();

        var token = new DeviceVerificationToken
        {
            Token = Guid.NewGuid().ToString("N"),
            EmployeeId = null,
            IsVerified = false
        };

        await InventoryEndpoints.ApplyVerificationAssignmentAsync(context, asset, token);
        await context.SaveChangesAsync();

        var updatedAsset = await context.ITAssets.FirstAsync(item => item.Id == asset.Id);
        var issue = await context.IssuedAssets.FirstOrDefaultAsync(item => item.ITAssetId == asset.Id);

        Assert.Equal(AssetStatus.Available, updatedAsset.Status);
        Assert.Null(issue);
    }
}
