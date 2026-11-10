// Project comment: OffboardingStep1Support.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Services;
using IT_Form.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.Offboarding.Step1
{
    /// <summary>
    /// Defines the OffboardingStep1Support class used by the IT Form application.
    /// </summary>
    internal static class OffboardingStep1Support
    {
        private static readonly string[] DamageExtensions = [".pdf", ".jpg", ".jpeg", ".png", ".mp4", ".mov", ".webm"];
        private static readonly string[] ReturnExtensions = [".pdf", ".jpg", ".jpeg", ".png"];
        private const long MaxDamageFileBytes = 50 * 1024 * 1024;
        private const long MaxReturnFileBytes = 10 * 1024 * 1024;

        /// <summary>
        /// Applies changes for the ApplyStep1Fields operation.
        /// </summary>
        internal static void ApplyStep1Fields(OffboardingRequest existing, OffboardingRequest request)
        {
            existing.AccountDisabledAD = request.AccountDisabledAD;
            existing.EmailForwarded = request.EmailForwarded;
            existing.ForwardToEmail = request.ForwardToEmail;
            existing.M365LicenseRevoked = request.M365LicenseRevoked;
            existing.SoftwareLicensesRevoked = request.SoftwareLicensesRevoked;
            existing.CheckPhysicalClean = request.CheckPhysicalClean;
            existing.CheckO365Deactivate = request.CheckO365Deactivate;
            existing.CheckWindowsDisable = request.CheckWindowsDisable;
            existing.CheckEmailSignOut = request.CheckEmailSignOut;
            existing.CheckSpecificSoftwareDeactivate = request.CheckSpecificSoftwareDeactivate;
            existing.CheckGeneralSoftwareDeactivate = request.CheckGeneralSoftwareDeactivate;
            existing.CheckAbcGroupRemove = request.CheckAbcGroupRemove;
            existing.CheckEmailMailGroupRemove = request.CheckEmailMailGroupRemove;
            existing.CheckDomainDisjoin = request.CheckDomainDisjoin;
            existing.CheckSharedDriveRemove = request.CheckSharedDriveRemove;
            existing.CheckWiFiDisable = request.CheckWiFiDisable;
            existing.CheckPrinterRemove = request.CheckPrinterRemove;
            existing.CheckScanFolderRemove = request.CheckScanFolderRemove;
            existing.CheckVPNDisable = request.CheckVPNDisable;
            existing.ChecklistRemarks = request.ChecklistRemarks;
            existing.DataBackupDone = request.DataBackupDone;
            existing.EmailBackupDone = request.EmailBackupDone;
            existing.EmailForwardingDone = request.EmailForwardingDone;
            existing.LicenseRemovalDone = request.LicenseRemovalDone;
            existing.LaptopReturned = request.LaptopReturned;
            existing.AccessoriesReturned = request.AccessoriesReturned;
            existing.IsHardwareDamaged = request.IsHardwareDamaged;
            existing.HardwareNotes = request.HardwareNotes;
            existing.MissingItemFines = request.MissingItemFines;
            existing.FinesRemarks = request.FinesRemarks;
        }

        /// <summary>
        /// Validates data for the ValidateDamageFile operation.
        /// </summary>
        internal static string? ValidateDamageFile(IFormFile? file)
        {
            if (file == null || file.Length <= 0)
            {
                return null;
            }

            if (file.Length > MaxDamageFileBytes)
            {
                return "Hardware proof file size exceeds the 50MB limit.";
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !DamageExtensions.Contains(extension))
            {
                return "Invalid file type. Allowed: PDF, images (.jpg, .jpeg, .png), and videos (.mp4, .mov, .webm).";
            }

            if (!UploadFileSignatureValidator.HasAllowedSignature(file, DamageExtensions))
            {
                return "Invalid file content. The file signature must match an allowed image, video, or PDF format.";
            }

            return null;
        }

        /// <summary>
        /// Validates data for the ValidateReturnFile operation.
        /// </summary>
        internal static string? ValidateReturnFile(IFormFile? file)
        {
            if (file == null || file.Length <= 0)
            {
                return null;
            }

            if (file.Length > MaxReturnFileBytes)
            {
                return "Hardware return file size exceeds the 10MB limit.";
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!ReturnExtensions.Contains(extension) ||
                !UploadFileSignatureValidator.HasAllowedSignature(file, ReturnExtensions))
            {
                return "Invalid return file type. Only valid PDF and JPEG/PNG files are allowed.";
            }

            return null;
        }

        /// <summary>
        /// Saves data for the SaveDamageFileAsync operation.
        /// </summary>
        internal static async Task<string?> SaveDamageFileAsync(
            IWebHostEnvironment env,
            OffboardingRequest existing,
            int requestId,
            IFormFile? hardwareDamageImage,
            CancellationToken cancellationToken)
        {
            if (hardwareDamageImage == null || hardwareDamageImage.Length <= 0)
            {
                return null;
            }

            var extension = Path.GetExtension(hardwareDamageImage.FileName).ToLowerInvariant();
            var fileName = $"Offboarding_Damage_{requestId}_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid()}{extension}";
            var previousPath = existing.HardwareDamageImagePath;
            if (extension is ".jpg" or ".jpeg" or ".png")
            {
                var reencoded = await ImageReencodingHelper.ReencodeAsync(hardwareDamageImage, cancellationToken);
                fileName = Path.ChangeExtension(fileName, reencoded.Extension);
                existing.HardwareDamageImagePath = await SensitiveFileStorage.SaveBytesAsync(
                    reencoded.Bytes, env, "offboarding", fileName, cancellationToken);
            }
            else
            {
                existing.HardwareDamageImagePath = await SensitiveFileStorage.SaveAsync(
                    hardwareDamageImage, env, "offboarding", fileName, cancellationToken);
            }

            // The caller deletes the previous file only after the new path is committed.
            return previousPath;
        }

        /// <summary>
        /// Saves data for the SaveReturnFileAsync operation.
        /// </summary>
        internal static async Task<string?> SaveReturnFileAsync(
            IWebHostEnvironment env,
            OffboardingRequest existing,
            int requestId,
            IFormFile? hardwareReturnImage,
            CancellationToken cancellationToken)
        {
            if (hardwareReturnImage == null || hardwareReturnImage.Length <= 0)
            {
                return null;
            }

            var extension = Path.GetExtension(hardwareReturnImage.FileName).ToLowerInvariant();
            if (!ReturnExtensions.Contains(extension) ||
                !UploadFileSignatureValidator.HasAllowedSignature(hardwareReturnImage, ReturnExtensions))
            {
                return null;
            }

            var fileName = $"Offboarding_Return_{requestId}_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid()}{extension}";
            var previousPath = existing.HardwareReturnImagePath;
            if (extension is ".jpg" or ".jpeg" or ".png")
            {
                var reencoded = await ImageReencodingHelper.ReencodeAsync(hardwareReturnImage, cancellationToken);
                fileName = Path.ChangeExtension(fileName, reencoded.Extension);
                existing.HardwareReturnImagePath = await SensitiveFileStorage.SaveBytesAsync(
                    reencoded.Bytes, env, "offboarding", fileName, cancellationToken);
            }
            else
            {
                existing.HardwareReturnImagePath = await SensitiveFileStorage.SaveAsync(
                    hardwareReturnImage, env, "offboarding", fileName, cancellationToken);
            }

            // The caller deletes the previous file only after the new path is committed.
            return previousPath;
        }

        /// <summary>
        /// Runs the MarkReturnedAssetsAsync operation.
        /// </summary>
        internal static async Task MarkReturnedAssetsAsync(
            ApplicationDbContext context,
            IReadOnlyCollection<int>? returnedAssetIds,
            CancellationToken cancellationToken)
        {
            if (returnedAssetIds == null || returnedAssetIds.Count == 0)
            {
                return;
            }

            var returnedAssets = await context.ITAssets
                .Where(a => returnedAssetIds.Contains(a.Id) && a.Status == AssetStatus.Issued)
                .ToListAsync(cancellationToken);

            foreach (var asset in returnedAssets)
            {
                asset.Status = AssetStatus.Available;
            }
        }

        /// <summary>
        /// Runs the FailureAsync operation.
        /// </summary>
        internal static async Task<ProcessResult<OffboardingStep1PageViewModel>> FailureAsync(
            ApplicationDbContext context,
            string message,
            OffboardingRequest request,
            CancellationToken cancellationToken)
        {
            return ProcessResult<OffboardingStep1PageViewModel>.Failure(message) with
            {
                Data = await BuildPageModelAsync(context, request, cancellationToken)
            };
        }

        /// <summary>
        /// Builds the value returned by the BuildPageModelAsync operation.
        /// </summary>
        private static async Task<OffboardingStep1PageViewModel> BuildPageModelAsync(
            ApplicationDbContext context,
            OffboardingRequest request,
            CancellationToken cancellationToken)
        {
            var companies = await context.Companies
                .Where(c => c.IsActive)
                .Select(c => string.IsNullOrEmpty(c.Lot) ? c.Name : $"{c.Name} ({c.Lot})")
                .ToListAsync(cancellationToken);

            return new OffboardingStep1PageViewModel(request)
            {
                Companies = companies
            };
        }
    }
}

