// Project comment: OnboardingItSetupSupport.cs is part of the IT Form application source code.

using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using IT_Form.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Onboarding.CompleteOnboardingItSetup
{
    /// <summary>
    /// Defines the OnboardingItSetupSupport class used by the IT Form application.
    /// </summary>
    internal static class OnboardingItSetupSupport
    {
        /// <summary>
        /// Applies changes for the ApplyChecklistToSession operation.
        /// </summary>
        internal static void ApplyChecklistToSession(OnboardingViewModel session, CompleteOnboardingItSetupCommand command)
        {
            session.CheckPhysicalClean = command.CheckPhysicalClean;
            session.CheckO365Setup = command.CheckO365Setup;
            session.CheckWindowsLogin = command.CheckWindowsLogin;
            session.CheckEmailSetup = command.CheckEmailSetup;
            session.CheckSpecificSoftware = command.CheckSpecificSoftware;
            session.CheckGeneralSoftware = command.CheckGeneralSoftware;
            session.CheckAbcGroup = command.CheckAbcGroup;
            session.CheckEmailMailGroup = command.CheckEmailMailGroup;
            session.CheckDomainJoin = command.CheckDomainJoin;
            session.CheckSharedDrive = command.CheckSharedDrive;
            session.CheckWiFiSetup = command.CheckWiFiSetup;
            session.CheckPrinterSetup = command.CheckPrinterSetup;
            session.CheckScanFolder = command.CheckScanFolder;
            session.CheckVPNSetup = command.CheckVPNSetup;
            session.ChecklistRemarks = command.ChecklistRemarks;
        }

        /// <summary>
        /// Runs the RebuildSoftwareAssignmentsAsync operation.
        /// </summary>
        internal static async Task<SoftwareAssignmentResult> RebuildSoftwareAssignmentsAsync(
            ApplicationDbContext context,
            OnboardingRequest onboardingRequest,
            OnboardingViewModel session,
            CancellationToken cancellationToken)
        {
            await OnboardingItSetupPersistence.ReplaceRequestSoftwareAsync(context, onboardingRequest, session, cancellationToken);
            await OnboardingItSetupPersistence.ReplaceRequestEmailGroupsAsync(context, onboardingRequest, session, cancellationToken);
            await OnboardingItSetupPersistence.ReplaceRequestSharedMailboxesAsync(context, onboardingRequest, session, cancellationToken);
            OnboardingItSetupPersistence.ApplySessionDetails(onboardingRequest, session);
            OnboardingItSetupPersistence.ReplaceFolderPermissions(context, onboardingRequest, session);

            var existingIssuedSoftware = await context.IssuedSoftware
                .Where(i => i.OnboardingRequestId == session.Id)
                .ToListAsync(cancellationToken);
            context.IssuedSoftware.RemoveRange(existingIssuedSoftware);

            var previouslyAssignedKeys = await context.SoftwareKeys
                .Where(k => k.OnboardingRequestId == session.Id)
                .ToListAsync(cancellationToken);
            foreach (var key in previouslyAssignedKeys)
            {
                key.IsAssigned = false;
                key.OnboardingRequestId = null;
            }

            var outOfStockLog = new List<string>();
            var newAssignmentsLog = new List<string>();

            if (session.SelectedSoftwareIds == null || !session.SelectedSoftwareIds.Any())
            {
                return new SoftwareAssignmentResult(newAssignmentsLog, outOfStockLog);
            }

            foreach (var softwareId in session.SelectedSoftwareIds.Distinct().Where(id => id > 0))
            {
                var software = await context.SoftwareLicenses.FindAsync(new object[] { softwareId }, cancellationToken);
                if (software == null)
                {
                    continue;
                }

                context.IssuedSoftware.Add(new IssuedSoftware
                {
                    SoftwareLicenseId = software.Id,
                    SoftwareName = software.SoftwareName,
                    OnboardingRequestId = session.Id
                });

                var availableKey = software.IsFree
                    ? null
                    : await context.SoftwareKeys
                        .Where(k => k.SoftwareLicenseId == software.Id && !k.IsAssigned)
                        .FirstOrDefaultAsync(cancellationToken);

                if (software.IsFree || availableKey != null)
                {
                    if (availableKey != null)
                    {
                        availableKey.IsAssigned = true;
                        availableKey.OnboardingRequestId = session.Id;
                    }

                    newAssignmentsLog.Add(software.SoftwareName);
                    continue;
                }

                outOfStockLog.Add(software.SoftwareName);
            }

            return new SoftwareAssignmentResult(newAssignmentsLog, outOfStockLog);
        }

        internal static async Task<string?> ValidateSelectedAssetsAsync(
            ApplicationDbContext context,
            OnboardingRequest onboardingRequest,
            OnboardingViewModel session,
            CancellationToken cancellationToken)
        {
            var selectedAssets = session.IssuedAssets?
                .Where(asset => asset.ITAssetId.HasValue && asset.ITAssetId.Value > 0)
                .ToList() ?? [];
            var selectedIds = selectedAssets.Select(asset => asset.ITAssetId!.Value).ToList();
            var distinctIds = selectedIds.Distinct().ToList();

            if (selectedIds.Count != distinctIds.Count)
            {
                return "The same inventory asset cannot be assigned more than once.";
            }

            if (distinctIds.Count == 0)
            {
                return null;
            }

            var currentRequestAssetIds = onboardingRequest.IssuedAssets
                .Where(asset => asset.ITAssetId.HasValue)
                .Select(asset => asset.ITAssetId!.Value)
                .ToHashSet();

            var assets = await context.ITAssets
                .Where(asset => distinctIds.Contains(asset.Id))
                .ToListAsync(cancellationToken);

            if (assets.Count != distinctIds.Count)
            {
                return "One or more selected inventory assets no longer exists or is unavailable.";
            }

            foreach (var asset in assets)
            {
                if (string.IsNullOrWhiteSpace(onboardingRequest.Branch) ||
                    !string.Equals(asset.Company, onboardingRequest.Branch, StringComparison.OrdinalIgnoreCase))
                {
                    return "An inventory asset can only be assigned within the onboarding request company scope.";
                }

                var isAvailable = asset.Status == AssetStatus.Available;
                var isAlreadyAssignedToThisRequest = asset.Status == AssetStatus.Issued && currentRequestAssetIds.Contains(asset.Id);
                if (!isAvailable && !isAlreadyAssignedToThisRequest)
                {
                    return $"Inventory asset '{asset.AssetTag}' is not available for assignment.";
                }
            }

            return null;
        }

        /// <summary>
        /// Runs the SyncIssuedAssetsAsync operation.
        /// </summary>
        internal static async Task<string?> SyncIssuedAssetsAsync(
            ApplicationDbContext context,
            OnboardingRequest onboardingRequest,
            OnboardingViewModel session,
            CancellationToken cancellationToken)
        {
            var oldItAssetIds = onboardingRequest.IssuedAssets
                .Where(x => x.ITAssetId.HasValue)
                .Select(x => x.ITAssetId!.Value)
                .ToList();

            context.IssuedAssets.RemoveRange(onboardingRequest.IssuedAssets);
            onboardingRequest.IssuedAssets.Clear();

            var newItAssetIds = new List<int>();
            if (session.IssuedAssets != null)
            {
                foreach (var asset in session.IssuedAssets)
                {
                    var newAsset = new IssuedAsset
                    {
                        AssetType = asset.AssetType,
                        OtherAssetType = asset.OtherAssetType,
                        Description = asset.Description,
                        Qty = asset.Qty,
                    };

                    if (asset.ITAssetId.HasValue && asset.ITAssetId.Value > 0)
                    {
                        var itAsset = await context.ITAssets.FindAsync(new object[] { asset.ITAssetId.Value }, cancellationToken);
                        if (itAsset != null)
                        {
                            newAsset.ITAssetId = itAsset.Id;
                            newAsset.OtherAssetType = !string.IsNullOrWhiteSpace(asset.OtherAssetType)
                                ? asset.OtherAssetType
                                : itAsset.OtherAssetType;
                            newAsset.AssetTag = itAsset.AssetTag;
                            newAsset.BrandModel = itAsset.Model ?? "N/A";
                            newAsset.SerialNumber = itAsset.SerialNumber ?? "N/A";
                            newAsset.Description = asset.Description ?? "Directly Assigned from Inventory";

                            if (!oldItAssetIds.Contains(itAsset.Id))
                            {
                                var claimed = await context.ITAssets
                                    .Where(candidate => candidate.Id == itAsset.Id &&
                                                        candidate.Status == AssetStatus.Available &&
                                                        candidate.Company == onboardingRequest.Branch)
                                    .ExecuteUpdateAsync(
                                        setters => setters.SetProperty(candidate => candidate.Status, AssetStatus.Issued),
                                        cancellationToken);

                                if (claimed != 1)
                                {
                                    return $"Inventory asset '{itAsset.AssetTag}' became unavailable while the assignment was being saved. Refresh and try again.";
                                }
                            }

                            itAsset.Status = AssetStatus.Issued;
                            newItAssetIds.Add(itAsset.Id);
                        }
                    }
                    else
                    {
                        newAsset.BrandModel = asset.BrandModel;
                        newAsset.SerialNumber = asset.SerialNumber;
                        newAsset.AssetTag = asset.AssetTag;
                    }

                    onboardingRequest.IssuedAssets.Add(newAsset);
                }
            }

            var unassignedAssetIds = oldItAssetIds.Except(newItAssetIds).ToList();
            if (!unassignedAssetIds.Any())
            {
                return null;
            }

            var unassignedAssets = await context.ITAssets
                .Where(a => unassignedAssetIds.Contains(a.Id))
                .ToListAsync(cancellationToken);
            foreach (var asset in unassignedAssets)
            {
                asset.Status = AssetStatus.Available;
            }

            return null;
        }

        /// <summary>
        /// Builds the value returned by the BuildSuccessMessage operation.
        /// </summary>
        internal static string BuildSuccessMessage(string fullName, IReadOnlyCollection<string> newAssignmentsLog)
        {
            var message = $"IT setup complete for {fullName}.";
            if (newAssignmentsLog.Any())
            {
                message += $" Auto-assigned keys for: {string.Join(", ", newAssignmentsLog)}.";
            }

            return message;
        }

        /// <summary>
        /// Builds the value returned by the BuildWarningMessage operation.
        /// </summary>
        internal static string? BuildWarningMessage(IReadOnlyCollection<string> outOfStockLog)
            => outOfStockLog.Any()
                ? $"Out of stock! Could not assign keys for: {string.Join(", ", outOfStockLog)}. Please add more keys in the Software Inventory."
                : null;
    }

    /// <summary>
    /// Represents the result returned by the SoftwareAssignment workflow.
    /// </summary>
    internal sealed record SoftwareAssignmentResult(
        IReadOnlyCollection<string> AssignedSoftwareNames,
        IReadOnlyCollection<string> OutOfStockSoftwareNames);
}

