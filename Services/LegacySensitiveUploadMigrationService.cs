using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Services
{
    public class LegacySensitiveUploadMigrationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<LegacySensitiveUploadMigrationService> _logger;

        public LegacySensitiveUploadMigrationService(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<LegacySensitiveUploadMigrationService> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        public async Task<int> MigrateAsync(CancellationToken cancellationToken = default)
        {
            var updatedRecords = 0;

            updatedRecords += await MigrateOffboardingFilesAsync(cancellationToken);

            if (updatedRecords > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Migrated {Count} legacy sensitive file reference(s) into secure storage.", updatedRecords);
            }

            return updatedRecords;
        }

        private async Task<int> MigrateOffboardingFilesAsync(CancellationToken cancellationToken)
        {
            var updated = 0;
            var requests = await _context.OffboardingRequests
                .Where(r =>
                    (r.ResignationLetterPath != null && r.ResignationLetterPath.StartsWith("/uploads/resignations/")) ||
                    (r.HardwareDamageImagePath != null && r.HardwareDamageImagePath.StartsWith("/uploads/offboarding/")) ||
                    (r.HardwareReturnImagePath != null && r.HardwareReturnImagePath.StartsWith("/uploads/offboarding/")))
                .ToListAsync(cancellationToken);

            foreach (var request in requests)
            {
                updated += TryMigratePath(
                    request,
                    r => r.ResignationLetterPath,
                    (r, value) => r.ResignationLetterPath = value,
                    SensitiveFileStorage.ResignationsCategory);

                updated += TryMigratePath(
                    request,
                    r => r.HardwareDamageImagePath,
                    (r, value) => r.HardwareDamageImagePath = value,
                    SensitiveFileStorage.OffboardingCategory);

                updated += TryMigratePath(
                    request,
                    r => r.HardwareReturnImagePath,
                    (r, value) => r.HardwareReturnImagePath = value,
                    SensitiveFileStorage.OffboardingCategory);
            }

            return updated;
        }

        private int TryMigratePath<T>(
            T entity,
            Func<T, string?> currentPathAccessor,
            Action<T, string?> currentPathSetter,
            string category)
        {
            var currentPath = currentPathAccessor(entity);
            if (string.IsNullOrWhiteSpace(currentPath))
            {
                return 0;
            }

            var sourcePhysicalPath = SensitiveFileStorage.ResolvePhysicalPath(_environment, currentPath);
            if (string.IsNullOrWhiteSpace(sourcePhysicalPath) || !File.Exists(sourcePhysicalPath))
            {
                _logger.LogWarning("Skipping legacy sensitive file migration because the source file was not found: {Path}", currentPath);
                return 0;
            }

            var updatedPath = SensitiveFileStorage.MoveExistingFileToSecureStorage(
                _environment,
                sourcePhysicalPath,
                category,
                Path.GetFileName(sourcePhysicalPath));

            currentPathSetter(entity, updatedPath);
            return 1;
        }
    }
}
