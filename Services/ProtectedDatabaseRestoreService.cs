// Project comment: ProtectedDatabaseRestoreService.cs restores protected SQLite backups before startup migrations.

using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace IT_Form.Services
{
    /// <summary>
    /// Restores an encrypted SQLite backup when no usable database is present at startup.
    /// </summary>
    public sealed class ProtectedDatabaseRestoreService
    {
        private const string BackupPurpose = "IT_Form.DatabaseBackups.v1";
        private const string ApplicationName = "IT_Form";
        private static readonly byte[] SqliteHeader = Encoding.ASCII.GetBytes("SQLite format 3\0");

        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ProtectedDatabaseRestoreService> _logger;

        public ProtectedDatabaseRestoreService(
            IDataProtectionProvider dataProtectionProvider,
            IWebHostEnvironment environment,
            ILogger<ProtectedDatabaseRestoreService> logger)
        {
            _dataProtectionProvider = dataProtectionProvider;
            _environment = environment;
            _logger = logger;
        }

        public bool TryRestoreIfNeeded(string databasePath, string dataDirectory)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
            {
                throw new InvalidOperationException("SQLite database path is unavailable.");
            }

            var targetPath = Path.GetFullPath(databasePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? dataDirectory);

            if (IsSqliteDatabase(targetPath))
            {
                return false;
            }

            var targetExists = File.Exists(targetPath);
            var targetLength = targetExists ? new FileInfo(targetPath).Length : 0;
            var candidates = GetProtectedCandidates(targetPath, dataDirectory, targetExists && targetLength > 0);
            if (candidates.Count == 0)
            {
                if (targetExists && targetLength > 0)
                {
                    throw new InvalidOperationException(
                        $"Database file '{targetPath}' exists but is not a valid SQLite database, and no protected backup was found.");
                }

                return false;
            }

            var protectors = CreateProtectors(dataDirectory);
            foreach (var candidate in candidates)
            {
                if (!TryUnprotectSqliteBackup(candidate, protectors, out var restoredBytes))
                {
                    continue;
                }

                RestoreDatabase(targetPath, restoredBytes, targetExists && targetLength > 0);
                _logger.LogInformation(
                    "Restored protected SQLite database backup {BackupPath} to {DatabasePath}.",
                    candidate.FullName,
                    targetPath);
                return true;
            }

            throw new InvalidOperationException(
                $"Protected database backup was detected for '{targetPath}', but it could not be decrypted with the available Data Protection keys.");
        }

        private IReadOnlyList<IDataProtector> CreateProtectors(string dataDirectory)
        {
            var protectors = new List<IDataProtector>
            {
                _dataProtectionProvider.CreateProtector(BackupPurpose)
            };

            foreach (var keyPath in GetFallbackKeyPaths(dataDirectory)
                .Where(HasDataProtectionKeys)
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var keyDirectory = new DirectoryInfo(keyPath);
                protectors.Add(DataProtectionProvider
                    .Create(keyDirectory, builder => builder.SetApplicationName(ApplicationName))
                    .CreateProtector(BackupPurpose));

                protectors.Add(DataProtectionProvider
                    .Create(keyDirectory)
                    .CreateProtector(BackupPurpose));
            }

            return protectors;
        }

        private IEnumerable<string> GetFallbackKeyPaths(string dataDirectory)
        {
            yield return Path.Combine(dataDirectory, "DataProtection-Keys");
            yield return Path.Combine(_environment.ContentRootPath, "App_Data", "DataProtection-Keys");

            var localAppDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrWhiteSpace(localAppDataRoot))
            {
                yield return Path.Combine(localAppDataRoot, ApplicationName, "DataProtection-Keys");
            }
        }

        private static bool HasDataProtectionKeys(string keyPath)
            => Directory.Exists(keyPath) && Directory.EnumerateFiles(keyPath, "*.xml").Any();

        private IReadOnlyList<FileInfo> GetProtectedCandidates(string targetPath, string dataDirectory, bool includeTarget)
        {
            var candidates = new List<FileInfo>();
            var knownPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void AddCandidate(string path)
            {
                if (!File.Exists(path))
                {
                    return;
                }

                var fullPath = Path.GetFullPath(path);
                if (knownPaths.Add(fullPath))
                {
                    candidates.Add(new FileInfo(fullPath));
                }
            }

            if (includeTarget)
            {
                AddCandidate(targetPath);
            }

            AddCandidate(targetPath + ".protected");

            var targetDirectory = Path.GetDirectoryName(targetPath);
            var protectedDirectories = new[]
            {
                targetDirectory,
                Path.Combine(dataDirectory, "backups")
            };
            var targetStem = Path.GetFileNameWithoutExtension(targetPath);

            foreach (var directory in protectedDirectories
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!Directory.Exists(directory))
                {
                    continue;
                }

                foreach (var file in new DirectoryInfo(directory)
                    .GetFiles($"{targetStem}*.db.protected")
                    .OrderByDescending(file => file.LastWriteTimeUtc))
                {
                    AddCandidate(file.FullName);
                }
            }

            return candidates;
        }

        private bool TryUnprotectSqliteBackup(FileInfo candidate, IReadOnlyList<IDataProtector> protectors, out byte[] restoredBytes)
        {
            restoredBytes = [];
            byte[] protectedBytes;
            try
            {
                protectedBytes = File.ReadAllBytes(candidate.FullName);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "Could not read protected database backup {BackupPath}.", candidate.FullName);
                return false;
            }

            foreach (var protector in protectors)
            {
                try
                {
                    var candidateBytes = protector.Unprotect(protectedBytes);
                    if (!HasSqliteHeader(candidateBytes))
                    {
                        _logger.LogWarning(
                            "Protected database backup {BackupPath} decrypted, but the payload is not a SQLite database.",
                            candidate.FullName);
                        continue;
                    }

                    restoredBytes = candidateBytes;
                    return true;
                }
                catch (CryptographicException)
                {
                    // Try the next key ring.
                }
            }

            return false;
        }

        private void RestoreDatabase(string targetPath, byte[] restoredBytes, bool preserveExistingTarget)
        {
            var tempPath = targetPath + $".restore-{Guid.NewGuid():N}.tmp";
            File.WriteAllBytes(tempPath, restoredBytes);

            try
            {
                DeleteSqliteSidecarFiles(targetPath);

                if (preserveExistingTarget && File.Exists(targetPath))
                {
                    var backupPath = targetPath + $".invalid-{DateTime.Now:yyyyMMdd_HHmmss}-{Guid.NewGuid():N}.bak";
                    File.Move(targetPath, backupPath);
                }
                else if (File.Exists(targetPath))
                {
                    File.Delete(targetPath);
                }

                File.Move(tempPath, targetPath);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        private void DeleteSqliteSidecarFiles(string targetPath)
        {
            foreach (var sidecarPath in new[] { targetPath + "-wal", targetPath + "-shm", targetPath + "-journal" })
            {
                if (!File.Exists(sidecarPath))
                {
                    continue;
                }

                try
                {
                    File.Delete(sidecarPath);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    _logger.LogWarning(ex, "Could not delete stale SQLite sidecar file {SidecarPath}.", sidecarPath);
                    throw;
                }
            }
        }

        private static bool IsSqliteDatabase(string path)
        {
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                var buffer = new byte[SqliteHeader.Length];
                var read = stream.Read(buffer, 0, buffer.Length);
                return read == SqliteHeader.Length && buffer.SequenceEqual(SqliteHeader);
            }
            catch (IOException)
            {
                return false;
            }
        }

        private static bool HasSqliteHeader(byte[] bytes)
            => bytes.Length >= SqliteHeader.Length &&
                bytes.AsSpan(0, SqliteHeader.Length).SequenceEqual(SqliteHeader);
    }
}
