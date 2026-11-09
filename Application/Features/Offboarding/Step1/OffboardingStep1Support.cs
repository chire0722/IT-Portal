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
            if (!DamageExtensions.Contains(extension) || !HasValidFileSignature(file, extension))
            {
                return "Hardware proof content does not match a supported PDF, image, or video format.";
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
            if (!ReturnExtensions.Contains(extension) || !HasValidFileSignature(file, extension))
            {
                return "Hardware return content does not match a supported PDF or image format.";
            }

            return null;
        }

        private static bool HasValidFileSignature(IFormFile file, string extension)
        {
            var contentType = file.ContentType?.Trim().ToLowerInvariant() ?? string.Empty;
            Span<byte> header = stackalloc byte[12];
            using var stream = file.OpenReadStream();
            var bytesRead = stream.Read(header);

            return extension switch
            {
                ".pdf" => contentType == "application/pdf" &&
                          bytesRead >= 5 &&
                          header[..5].SequenceEqual("%PDF-"u8),
                ".jpg" or ".jpeg" => contentType == "image/jpeg" &&
                                     bytesRead >= 3 &&
                                     header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
                ".png" => contentType == "image/png" &&
                          bytesRead >= 8 &&
                          header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
                ".mp4" => contentType == "video/mp4" &&
                          bytesRead >= 12 &&
                          header[4..8].SequenceEqual("ftyp"u8),
                ".mov" => contentType == "video/quicktime" &&
                          bytesRead >= 12 &&
                          header[4..8].SequenceEqual("ftyp"u8) &&
                          header[8..12].SequenceEqual("qt  "u8),
                ".webm" => contentType == "video/webm" &&
                           bytesRead >= 4 &&
                           header[..4].SequenceEqual(new byte[] { 0x1A, 0x45, 0xDF, 0xA3 }),
                _ => false
            };
        }

        /// <summary>
        /// Saves data for the SaveDamageFileAsync operation.
        /// </summary>
        internal static async Task SaveDamageFileAsync(
            IWebHostEnvironment env,
            OffboardingRequest existing,
            int requestId,
            IFormFile? hardwareDamageImage,
            CancellationToken cancellationToken)
        {
            if (hardwareDamageImage == null || hardwareDamageImage.Length <= 0)
            {
                return;
            }

            var extension = Path.GetExtension(hardwareDamageImage.FileName).ToLowerInvariant();
            var fileName = $"Offboarding_Damage_{requestId}_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid()}{extension}";
            existing.HardwareDamageImagePath = await SensitiveFileStorage.SaveAsync(
                hardwareDamageImage,
                env,
                "offboarding",
                fileName,
                cancellationToken);
        }

        /// <summary>
        /// Saves data for the SaveReturnFileAsync operation.
        /// </summary>
        internal static async Task SaveReturnFileAsync(
            IWebHostEnvironment env,
            OffboardingRequest existing,
            int requestId,
            IFormFile? hardwareReturnImage,
            CancellationToken cancellationToken)
        {
            if (hardwareReturnImage == null || hardwareReturnImage.Length <= 0)
            {
                return;
            }

            var extension = Path.GetExtension(hardwareReturnImage.FileName).ToLowerInvariant();
            var contentType = hardwareReturnImage.ContentType?.ToLowerInvariant() ?? string.Empty;
            if (!ReturnExtensions.Contains(extension) ||
                (!contentType.StartsWith("image/") && contentType != "application/pdf"))
            {
                return;
            }

            var fileName = $"Offboarding_Return_{requestId}_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid()}{extension}";
            existing.HardwareReturnImagePath = await SensitiveFileStorage.SaveAsync(
                hardwareReturnImage,
                env,
                "offboarding",
                fileName,
                cancellationToken);
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
