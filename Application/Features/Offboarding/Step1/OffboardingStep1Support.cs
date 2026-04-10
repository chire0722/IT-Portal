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
    internal static class OffboardingStep1Support
    {
        private static readonly string[] DamageExtensions = [".pdf", ".jpg", ".jpeg", ".png", ".mp4", ".mov", ".webm"];
        private static readonly string[] ReturnExtensions = [".pdf", ".jpg", ".jpeg", ".png"];

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

        internal static string? ValidateDamageFile(IFormFile? file)
        {
            if (file == null || file.Length <= 0)
            {
                return null;
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!string.IsNullOrEmpty(extension) && !DamageExtensions.Contains(extension))
            {
                return "Invalid file type. Allowed: PDF, images (.jpg, .jpeg, .png), and videos (.mp4, .mov, .webm).";
            }

            var contentType = file.ContentType.ToLowerInvariant();
            if (!contentType.StartsWith("image/") &&
                !contentType.StartsWith("video/") &&
                contentType != "application/pdf")
            {
                return "Invalid file content. Only image, video, or PDF files are allowed for hardware proof.";
            }

            return null;
        }

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
            SensitiveFileStorage.DeleteStoredFile(env, existing.HardwareDamageImagePath);
            existing.HardwareDamageImagePath = await SensitiveFileStorage.SaveAsync(
                hardwareDamageImage,
                env,
                "offboarding",
                fileName,
                cancellationToken);
        }

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
            var contentType = hardwareReturnImage.ContentType.ToLowerInvariant();
            if (!ReturnExtensions.Contains(extension) ||
                (!contentType.StartsWith("image/") && contentType != "application/pdf"))
            {
                return;
            }

            var fileName = $"Offboarding_Return_{requestId}_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid()}{extension}";
            SensitiveFileStorage.DeleteStoredFile(env, existing.HardwareReturnImagePath);
            existing.HardwareReturnImagePath = await SensitiveFileStorage.SaveAsync(
                hardwareReturnImage,
                env,
                "offboarding",
                fileName,
                cancellationToken);
        }

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
