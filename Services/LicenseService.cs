// Project comment: LicenseService.cs is part of the IT Form application source code.

using IT_Form.Data;
using System.Security.Cryptography;
using System.Text;
using IT_Form.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace IT_Form.Services
{
    /// <summary>
    /// Provides ILicense service behavior for the application.
    /// </summary>
    public interface ILicenseService
    {
        Task<(bool Success, string Message)> AssignKeyAsync(int licenseId, string? userId, string? machineName = null);
        Task<(bool Success, string Message, int ImportedCount)> ImportKeysAsync(int licenseId, List<string> keys);
        Task<int> GetAvailableSeatCountAsync(int licenseId);
    }

    /// <summary>
    /// Provides License service behavior for the application.
    /// </summary>
    public class LicenseService : ILicenseService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILicenseEncryptionService _encryptionService;

        public LicenseService(ApplicationDbContext context, ILicenseEncryptionService encryptionService)
        {
            _context = context;
            _encryptionService = encryptionService;
        }

        public async Task<(bool Success, string Message)> AssignKeyAsync(int licenseId, string? userId, string? machineName = null)
        {
            var ownsTransaction = _context.Database.CurrentTransaction == null;
            IDbContextTransaction? transaction = null;

            if (ownsTransaction)
            {
                transaction = await _context.Database.BeginTransactionAsync();
            }

            try
            {
                var license = await _context.SoftwareLicenses
                    .Include(l => l.Keys)
                    .FirstOrDefaultAsync(l => l.Id == licenseId);

                if (license == null) return (false, "License not found.");

                var availableKey = license.Keys.FirstOrDefault(k => !k.IsAssigned);
                if (availableKey == null)
                {
                    // Check if we can create a "SaaS" seat (no specific key)
                    int assignedCount = license.Keys.Count(k => k.IsAssigned);
                    if (assignedCount < license.ContractLimit)
                    {
                        availableKey = new SoftwareKey
                        {
                            SoftwareLicenseId = licenseId,
                            IsAssigned = true,
                            AssignedToUserId = userId,
                            MachineName = machineName,
                            AssignmentDate = DateTime.UtcNow
                        };
                        _context.SoftwareKeys.Add(availableKey);
                    }
                    else
                    {
                        return (false, "No available seats or keys.");
                    }
                }
                else
                {
                    availableKey.IsAssigned = true;
                    availableKey.AssignedToUserId = userId;
                    availableKey.MachineName = machineName;
                    availableKey.AssignmentDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                if (transaction != null)
                {
                    await transaction.CommitAsync();
                }

                return (true, "License assigned successfully.");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }

                return (false, "Concurrency conflict. Please try again.");
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }

                return (false, $"Error: {ex.Message}");
            }
            finally
            {
                if (transaction != null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }

        public async Task<(bool Success, string Message, int ImportedCount)> ImportKeysAsync(int licenseId, List<string> rawKeys)
        {
            if (rawKeys == null || !rawKeys.Any()) return (false, "No keys provided.", 0);

            var ownsTransaction = _context.Database.CurrentTransaction == null;
            IDbContextTransaction? transaction = null;

            if (ownsTransaction)
            {
                transaction = await _context.Database.BeginTransactionAsync();
            }

            try
            {
                int imported = 0;
                foreach (var rawKey in rawKeys.Where(k => !string.IsNullOrWhiteSpace(k)))
                {
                    string encrypted = _encryptionService.EncryptKey(rawKey.Trim());
                    string hash = ComputeHash(rawKey.Trim());

                    // Idempotency check with hash
                    bool exists = await _context.SoftwareKeys.AnyAsync(k => k.KeyValueHash == hash);
                    if (exists) continue;

                    _context.SoftwareKeys.Add(new SoftwareKey
                    {
                        SoftwareLicenseId = licenseId,
                        KeyValue = encrypted,
                        KeyValueHash = hash,
                        IsAssigned = false,
                        CreatedAt = DateTime.UtcNow
                    });
                    imported++;
                }

                await _context.SaveChangesAsync();
                if (transaction != null)
                {
                    await transaction.CommitAsync();
                }

                return (true, $"{imported} keys imported.", imported);
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }

                return (false, $"Import failed: {ex.Message}", 0);
            }
            finally
            {
                if (transaction != null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }

        /// <summary>
        /// Gets the data required by the GetAvailableSeatCountAsync operation.
        /// </summary>
        public async Task<int> GetAvailableSeatCountAsync(int licenseId)
        {
            var license = await _context.SoftwareLicenses
                .AsNoTracking()
                .Include(l => l.Keys)
                .FirstOrDefaultAsync(l => l.Id == licenseId);

            if (license == null) return 0;

            int assigned = license.Keys.Count(k => k.IsAssigned);
            return Math.Max(0, license.ContractLimit - assigned);
        }

        /// <summary>
        /// Runs the ComputeHash operation.
        /// </summary>
        private string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }
    }
}
