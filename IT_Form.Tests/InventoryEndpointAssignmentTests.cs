// Project comment: InventoryEndpointAssignmentTests.cs is part of the IT Form application source code.

using System;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Tests;

/// <summary>
/// Defines the InventoryEndpointAssignmentTests class used by the IT Form application.
/// </summary>
public sealed class InventoryEndpointAssignmentTests : ControllerTestBase
{
    [Fact]
    /// <summary>
    /// Runs the ResolveVerificationAssignmentAsync_MatchesCurrentEmployee_ByEmail operation.
    /// </summary>
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
    /// <summary>
    /// Runs the ResolveVerificationAssignmentAsync_FallsBackToManualName_WhenCurrentEmployeeRecordMissing operation.
    /// </summary>
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
    /// <summary>
    /// Applies changes for the ApplyVerificationAssignmentAsync_AssignsAssetToSelectedEmployee operation.
    /// </summary>
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
    /// <summary>
    /// Applies changes for the ApplyVerificationAssignmentAsync_DoesNothing_WhenTokenHasNoEmployee operation.
    /// </summary>
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

    [Fact]
    /// <summary>
    /// Applies changes for the ApplyHardwareFingerprintTelemetry_CapturesFirstFingerprint_AsBaselineAndLatest operation.
    /// </summary>
    public void ApplyHardwareFingerprintTelemetry_CapturesFirstFingerprint_AsBaselineAndLatest()
    {
        var asset = new ITAsset
        {
            AssetTag = "LAP-001"
        };
        var observedAt = new DateTime(2026, 4, 14, 1, 0, 0, DateTimeKind.Utc);

        var result = InventoryEndpoints.ApplyHardwareFingerprintTelemetry(
            asset,
            "HASH-001",
            "{\"GeneratedAtUtc\":\"2026-04-14T01:00:00Z\"}",
            observedAt);

        Assert.False(result.IsMismatch);
        Assert.Null(result.WarningMessage);
        Assert.Equal("HASH-001", asset.HardwareHash);
        Assert.Equal("HASH-001", asset.LatestHardwareHash);
        Assert.Equal("{\"GeneratedAtUtc\":\"2026-04-14T01:00:00Z\"}", asset.HardwareAuditJson);
        Assert.Equal("{\"GeneratedAtUtc\":\"2026-04-14T01:00:00Z\"}", asset.LatestHardwareAuditJson);
        Assert.False(asset.HasHardwareFingerprintMismatch);
        Assert.Null(asset.HardwareFingerprintMismatchDetectedAt);
    }

    [Fact]
    /// <summary>
    /// Applies changes for the ApplyHardwareFingerprintTelemetry_FlagsWarning_WhenLatestFingerprintDiffersFromBaseline operation.
    /// </summary>
    public void ApplyHardwareFingerprintTelemetry_FlagsWarning_WhenLatestFingerprintDiffersFromBaseline()
    {
        var asset = new ITAsset
        {
            AssetTag = "LAP-002",
            HardwareHash = "HASH-BASELINE",
            HardwareAuditJson = "{\"GeneratedAtUtc\":\"2026-04-13T10:00:00Z\"}"
        };
        var observedAt = new DateTime(2026, 4, 14, 2, 30, 0, DateTimeKind.Utc);

        var result = InventoryEndpoints.ApplyHardwareFingerprintTelemetry(
            asset,
            "HASH-CHANGED",
            "{\"GeneratedAtUtc\":\"2026-04-14T02:30:00Z\"}",
            observedAt);

        Assert.True(result.IsMismatch);
        Assert.NotNull(result.WarningMessage);
        Assert.Contains("LAP-002", result.WarningMessage, StringComparison.Ordinal);
        Assert.Equal("HASH-BASELINE", asset.HardwareHash);
        Assert.Equal("HASH-CHANGED", asset.LatestHardwareHash);
        Assert.Equal("{\"GeneratedAtUtc\":\"2026-04-14T02:30:00Z\"}", asset.LatestHardwareAuditJson);
        Assert.True(asset.HasHardwareFingerprintMismatch);
        Assert.Equal(observedAt, asset.HardwareFingerprintMismatchDetectedAt);
    }

    [Fact]
    /// <summary>
    /// Applies changes for the ApplyHardwareFingerprintTelemetry_ClearsExistingWarning_WhenLatestFingerprintMatchesBaseline operation.
    /// </summary>
    public void ApplyHardwareFingerprintTelemetry_ClearsExistingWarning_WhenLatestFingerprintMatchesBaseline()
    {
        var asset = new ITAsset
        {
            AssetTag = "LAP-003",
            HardwareHash = "HASH-SAME",
            HardwareAuditJson = "{\"GeneratedAtUtc\":\"2026-04-13T10:00:00Z\"}",
            LatestHardwareHash = "HASH-CHANGED",
            HasHardwareFingerprintMismatch = true,
            HardwareFingerprintMismatchDetectedAt = new DateTime(2026, 4, 13, 10, 5, 0, DateTimeKind.Utc),
            HardwareFingerprintWarning = "Old warning"
        };
        var observedAt = new DateTime(2026, 4, 14, 3, 15, 0, DateTimeKind.Utc);

        var result = InventoryEndpoints.ApplyHardwareFingerprintTelemetry(
            asset,
            "HASH-SAME",
            "{\"GeneratedAtUtc\":\"2026-04-14T03:15:00Z\"}",
            observedAt);

        Assert.False(result.IsMismatch);
        Assert.Null(result.WarningMessage);
        Assert.Equal("HASH-SAME", asset.LatestHardwareHash);
        Assert.Equal("{\"GeneratedAtUtc\":\"2026-04-14T03:15:00Z\"}", asset.LatestHardwareAuditJson);
        Assert.False(asset.HasHardwareFingerprintMismatch);
        Assert.Null(asset.HardwareFingerprintMismatchDetectedAt);
        Assert.Null(asset.HardwareFingerprintWarning);
    }
}
