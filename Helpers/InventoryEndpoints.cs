// Project comment: InventoryEndpoints.cs is part of the IT Form application source code.

using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace IT_Form.Helpers
{
    /// <summary>
    /// Defines the InventoryEndpoints class used by the IT Form application.
    /// </summary>
    public static class InventoryEndpoints
    {
        /// <summary>
        /// Maps data for the MapInventoryEndpoints operation.
        /// </summary>
        public static void MapInventoryEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/api/inventory/sync", async (HardwareReport report, HttpContext httpContext, ApplicationDbContext dbContext, IConfiguration config, ILoggerFactory loggerFactory) =>
            {
                var logger = loggerFactory.CreateLogger("IT_Form.InventorySync");
                bool isAuthenticatedByKey = false;
                var expectedKey = ConfigurationValueNormalizer.NormalizeSecret(config["InventorySync:ApiKey"]);
                var allowLegacyInventoryApiKey = bool.TryParse(config["SecuritySettings:AllowLegacyInventoryApiKey"], out var enabled) && enabled;

                if (allowLegacyInventoryApiKey &&
                    httpContext.Request.Headers.TryGetValue("X-Inventory-Key", out var providedKey) &&
                    !string.IsNullOrEmpty(expectedKey) &&
                    System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                        System.Text.Encoding.UTF8.GetBytes(providedKey.ToString()),
                        System.Text.Encoding.UTF8.GetBytes(expectedKey)))
                {
                    isAuthenticatedByKey = true;
                }

                DeviceVerificationToken? dbToken = null;
                var tokenLifetimeMinutes = int.TryParse(config["SecuritySettings:DeviceVerificationExpiryMinutes"], out var configuredMinutes) && configuredMinutes > 0
                    ? configuredMinutes
                    : 30;
                if (!isAuthenticatedByKey)
                {
                    if (string.IsNullOrWhiteSpace(report.Token))
                    {
                        return Results.Unauthorized();
                    }

                    dbToken = await dbContext.DeviceVerificationTokens.FirstOrDefaultAsync(t => t.Token == report.Token);
                    if (dbToken == null || dbToken.IsVerified)
                    {
                        return Results.Unauthorized();
                    }

                    if (HasExpiredVerificationToken(dbToken, tokenLifetimeMinutes))
                    {
                        return Results.Unauthorized();
                    }
                }

                var shouldAssignVerifiedAsset = dbToken == null || !string.IsNullOrWhiteSpace(dbToken.EmployeeId);

                if (string.IsNullOrWhiteSpace(report.SN))
                {
                    return Results.BadRequest(new { message = "Serial Number (SN) is required." });
                }

                // Find active IT asset by Serial Number (respecting soft-delete filter).
                var asset = await dbContext.ITAssets.FirstOrDefaultAsync(a => a.SerialNumber == report.SN);

                bool isNew = false;
                if (asset == null)
                {
                    isNew = true;
                    var assetType = AssetType.DesktopPC;
                    // Determine Asset Type based on OS and Model
                    if (!string.IsNullOrEmpty(report.OS) && report.OS.Contains("Server", StringComparison.OrdinalIgnoreCase))
                    {
                        assetType = AssetType.Others;
                    }
                    else if (!string.IsNullOrEmpty(report.Model) && (report.Model.Contains("Laptop", StringComparison.OrdinalIgnoreCase) || report.Model.Contains("Book", StringComparison.OrdinalIgnoreCase)))
                    {
                        assetType = AssetType.Laptop;
                    }
                    else
                    {
                        assetType = AssetType.DesktopPC; // Default
                    }

                    var baseAssetTag = string.IsNullOrWhiteSpace(report.ComputerName) ? $"AUTO-{report.SN}" : report.ComputerName;
                    var assetTag = baseAssetTag;
                    int count = 1;

                    // Check for active collisions only
                    while (await dbContext.ITAssets.AnyAsync(a => a.AssetTag == assetTag))
                    {
                        assetTag = $"{baseAssetTag}-{count++}";
                    }

                    asset = new ITAsset
                    {
                        AssetTag = assetTag,
                        SerialNumber = report.SN,
                        Status = shouldAssignVerifiedAsset ? AssetStatus.Issued : AssetStatus.Available,
                        AssetType = assetType
                    };
                    dbContext.ITAssets.Add(asset);
                }
                else
                {
                    // Handle computer name change (Tag sync)
                    if (!string.IsNullOrWhiteSpace(report.ComputerName) && asset.AssetTag != report.ComputerName)
                    {
                        // Only change tag if the new tag is NOT taken by ANOTHER active asset
                        var tagCollision = await dbContext.ITAssets.AnyAsync(a => a.AssetTag == report.ComputerName && a.Id != asset.Id);
                        if (!tagCollision)
                        {
                            asset.AssetTag = report.ComputerName;
                        }
                    }
                }

                // Update Telemetry
                asset.Model = report.Model;
                asset.CPU = report.CPU;
                asset.RAM = report.RAM_GB;
                asset.Storage = report.Storage;
                asset.DDRType = report.DDRType;
                asset.WindowsVersion = report.OS;
                var hardwareAuditJson = report.Audit.HasValue &&
                                        report.Audit.Value.ValueKind != JsonValueKind.Null &&
                                        report.Audit.Value.ValueKind != JsonValueKind.Undefined
                    ? report.Audit.Value.GetRawText()
                    : null;

                var fingerprintSync = ApplyHardwareFingerprintTelemetry(
                    asset,
                    report.HardwareHash,
                    hardwareAuditJson,
                    DateTime.UtcNow);

                asset.LastSeen = DateTime.UtcNow;

                if (dbToken != null)
                {
                    dbToken.IsVerified = true;
                    dbToken.VerifiedAt = DateTime.UtcNow;
                    dbToken.VerifiedAssetTag = asset.AssetTag;
                    dbToken.HasHardwareFingerprintMismatch = fingerprintSync.IsMismatch;
                    dbToken.HardwareFingerprintWarning = fingerprintSync.WarningMessage;
                    if (shouldAssignVerifiedAsset)
                    {
                        await ApplyVerificationAssignmentAsync(dbContext, asset, dbToken);
                    }
                }

                try
                {
                    await dbContext.SaveChangesAsync();
                }
                catch (DbUpdateException ex)
                {
                    logger.LogError(ex,
                        "Inventory sync could not persist telemetry for serial number {SerialNumber}.",
                        report.SN);
                    return Results.Problem(
                        "Inventory sync could not be completed. Please retry or contact IT support.",
                        statusCode: StatusCodes.Status409Conflict);
                }

            return Results.Ok(new
            {
                message = isNew ? "Asset registered and synchronized." : "Asset telemetry updated.",
                assetTag = asset.AssetTag,
                fingerprintMismatch = fingerprintSync.IsMismatch,
                warningMessage = fingerprintSync.WarningMessage
            });
        })
            .AllowAnonymous()
            .WithName("SyncInventory")
            .RequireRateLimiting("strict");
        }

        /// <summary>
        /// Applies changes for the ApplyVerificationAssignmentAsync operation.
        /// </summary>
        public static async Task ApplyVerificationAssignmentAsync(
            ApplicationDbContext dbContext,
            ITAsset asset,
            DeviceVerificationToken token)
        {
            var assignment = await ResolveVerificationAssignmentAsync(dbContext, token.EmployeeId);
            if (assignment == null)
            {
                return;
            }

            asset.Status = AssetStatus.Issued;

            var latestIssue = await dbContext.IssuedAssets
                .AsNoTracking()
                .OrderByDescending(issuedAsset => issuedAsset.Id)
                .FirstOrDefaultAsync(issuedAsset => issuedAsset.ITAssetId == asset.Id);

            var matchesExistingAssignment =
                assignment.OnboardingRequestId.HasValue
                    ? latestIssue?.OnboardingRequestId == assignment.OnboardingRequestId.Value
                    : latestIssue?.OnboardingRequestId == null &&
                      string.Equals(latestIssue?.CustomEmployeeName, assignment.CustomEmployeeName, StringComparison.OrdinalIgnoreCase);

            if (matchesExistingAssignment)
            {
                return;
            }

            dbContext.IssuedAssets.Add(new IssuedAsset
            {
                OnboardingRequestId = assignment.OnboardingRequestId,
                CustomEmployeeName = assignment.CustomEmployeeName,
                ITAsset = asset,
                AssetType = asset.AssetType,
                OtherAssetType = asset.OtherAssetType,
                SerialNumber = asset.SerialNumber,
                AssetTag = asset.AssetTag,
                Qty = 1,
                BrandModel = asset.Model
            });
        }

    /// <summary>
    /// Runs the ResolveVerificationAssignmentAsync operation.
    /// </summary>
    public static async Task<InventoryVerificationAssignment?> ResolveVerificationAssignmentAsync(
        ApplicationDbContext dbContext,
        string? applicationUserId)
        {
            if (string.IsNullOrWhiteSpace(applicationUserId))
            {
                return null;
            }

            var user = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == applicationUserId);

            if (user == null)
            {
                return null;
            }

            var currentEmployees = await dbContext.OnboardingRequests
                .AsNoTracking()
                .Where(request => request.Status == RequestStatus.Completed || request.Status == RequestStatus.Approved)
                .OrderByDescending(request => request.CompletedAt ?? request.RequestDate)
                .ToListAsync();

            OnboardingRequest? onboardingRequest = null;
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                onboardingRequest = currentEmployees.FirstOrDefault(request =>
                    string.Equals(request.EmailAddress, user.Email, StringComparison.OrdinalIgnoreCase));
            }

            if (onboardingRequest == null && !string.IsNullOrWhiteSpace(user.UserName))
            {
                onboardingRequest = currentEmployees.FirstOrDefault(request =>
                    string.Equals(request.EmailAddress, user.UserName, StringComparison.OrdinalIgnoreCase));
            }

            if (onboardingRequest == null && !string.IsNullOrWhiteSpace(user.FullName))
            {
                onboardingRequest = currentEmployees.FirstOrDefault(request =>
                    string.Equals(request.FullName, user.FullName, StringComparison.OrdinalIgnoreCase));
            }

            return onboardingRequest != null
                ? new InventoryVerificationAssignment(onboardingRequest.Id, null)
                : !string.IsNullOrWhiteSpace(user.FullName)
                    ? new InventoryVerificationAssignment(null, user.FullName)
                    : null;
        }

        /// <summary>
        /// Runs the HasExpiredVerificationToken operation.
        /// </summary>
        private static bool HasExpiredVerificationToken(DeviceVerificationToken token, int tokenLifetimeMinutes)
            => !string.IsNullOrWhiteSpace(token.EmployeeId) &&
               token.CreatedAt.AddMinutes(tokenLifetimeMinutes) <= DateTime.UtcNow;

        /// <summary>
        /// Applies changes for the ApplyHardwareFingerprintTelemetry operation.
        /// </summary>
        public static HardwareFingerprintSyncResult ApplyHardwareFingerprintTelemetry(
            ITAsset asset,
            string? incomingHardwareHash,
            string? incomingHardwareAuditJson,
            DateTime observedAtUtc)
        {
            ArgumentNullException.ThrowIfNull(asset);

            var normalizedIncomingHash = NormalizeFingerprintValue(incomingHardwareHash);
            var normalizedIncomingAuditJson = NormalizeFingerprintValue(incomingHardwareAuditJson);

            if (!string.IsNullOrWhiteSpace(normalizedIncomingHash))
            {
                if (string.IsNullOrWhiteSpace(asset.HardwareHash))
                {
                    asset.HardwareHash = normalizedIncomingHash;
                }

                asset.LatestHardwareHash = normalizedIncomingHash;
            }

            if (!string.IsNullOrWhiteSpace(normalizedIncomingAuditJson))
            {
                if (string.IsNullOrWhiteSpace(asset.HardwareAuditJson))
                {
                    asset.HardwareAuditJson = normalizedIncomingAuditJson;
                }

                asset.LatestHardwareAuditJson = normalizedIncomingAuditJson;
            }

            var hasComparableFingerprint =
                !string.IsNullOrWhiteSpace(asset.HardwareHash) &&
                !string.IsNullOrWhiteSpace(normalizedIncomingHash);

            var isMismatch = hasComparableFingerprint &&
                             !string.Equals(asset.HardwareHash, normalizedIncomingHash, StringComparison.OrdinalIgnoreCase);

            if (isMismatch)
            {
                asset.HasHardwareFingerprintMismatch = true;
                asset.HardwareFingerprintMismatchDetectedAt = observedAtUtc;
                asset.HardwareFingerprintWarning = BuildHardwareFingerprintWarning(asset);
            }
            else if (hasComparableFingerprint || string.IsNullOrWhiteSpace(asset.HardwareHash))
            {
                asset.HasHardwareFingerprintMismatch = false;
                asset.HardwareFingerprintMismatchDetectedAt = null;
                asset.HardwareFingerprintWarning = null;
            }

            return new HardwareFingerprintSyncResult(
                asset.HasHardwareFingerprintMismatch,
                asset.HardwareFingerprintWarning);
        }

        /// <summary>
        /// Runs the NormalizeFingerprintValue operation.
        /// </summary>
        private static string? NormalizeFingerprintValue(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        /// <summary>
        /// Builds the value returned by the BuildHardwareFingerprintWarning operation.
        /// </summary>
        private static string BuildHardwareFingerprintWarning(ITAsset asset)
        {
            var assetLabel = string.IsNullOrWhiteSpace(asset.AssetTag)
                ? "this asset"
                : $"asset {asset.AssetTag}";

            return $"Warning: the latest hardware fingerprint does not match the first verified fingerprint stored for {assetLabel}. Review the device before trusting this verification.";
        }
    }

    /// <summary>
    /// Defines the HardwareReport record used by the IT Form application.
    /// </summary>
    public record HardwareReport(
        string? EmployeeId,
        string SN,
        string Model,
        string CPU,
        string RAM_GB,
        string OS,
        string? Storage = null,
        string? DDRType = null,
        string? Token = null,
        string? ComputerName = null,
        string? HardwareHash = null,
        JsonElement? Audit = null);

    /// <summary>
    /// Defines the InventoryVerificationAssignment record used by the IT Form application.
    /// </summary>
    public sealed record InventoryVerificationAssignment(
        int? OnboardingRequestId,
        string? CustomEmployeeName);

    /// <summary>
    /// Represents the result returned by the HardwareFingerprintSync workflow.
    /// </summary>
    public sealed record HardwareFingerprintSyncResult(
        bool IsMismatch,
        string? WarningMessage);
}
