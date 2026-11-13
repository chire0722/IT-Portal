// Project comment: DatabaseBackupService.cs is part of the IT Form application source code.

using System.IO;
using System.Linq;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Services
{
    /// <summary>
    /// Creates encrypted SQLite database backups on startup and on a daily schedule.
    /// </summary>
    public class DatabaseBackupService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseBackupService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IDataProtector _backupProtector;

        /// <summary>
        /// Creates the backup worker and its data protector for encrypted backup files.
        /// </summary>
        public DatabaseBackupService(
            IConfiguration configuration,
            ILogger<DatabaseBackupService> logger,
            IWebHostEnvironment environment,
            IServiceScopeFactory scopeFactory,
            IDataProtectionProvider dataProtectionProvider)
        {
            _configuration = configuration;
            _logger = logger;
            _environment = environment;
            _scopeFactory = scopeFactory;
            _backupProtector = dataProtectionProvider.CreateProtector("IT_Form.DatabaseBackups.v1");
        }

        /// <summary>
        /// Runs one startup backup, then schedules recurring backups for 2:00 AM local server time.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Create an immediate restore point whenever the application starts.
            try
            {
                RunBackup();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to perform initial database backup.");
                await NotifyITAsync($"Initial startup database backup failed: {ex.Message}");
            }

            // Keep scheduling the next daily backup until the host is shutting down.
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;
                    var nextRunTime = now.Date.AddDays(1).AddHours(2);
                    
                    // Before 2:00 AM, schedule today's run; otherwise schedule tomorrow's run.
                    if (now.Hour < 2)
                    {
                        nextRunTime = now.Date.AddHours(2);
                    }

                    var delay = nextRunTime - now;
                    _logger.LogInformation("Next scheduled backup at {Time}. Waiting for {Delay}...", nextRunTime, delay);

                    await Task.Delay(delay, stoppingToken);

                    _logger.LogInformation("Starting scheduled daily database backup...");
                    RunBackup();
                }
                catch (OperationCanceledException)
                {
                    // The host requested shutdown through the cancellation token.
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Periodic daily database backup failed.");
                    await NotifyITAsync($"Periodic scheduled database backup failed: {ex.Message}");
                    
                    // Back off after a failure so logs and IT notifications are not spammed.
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }

        /// <summary>
        /// Resolves the configured SQLite database, creates an encrypted backup, and copies it to all configured locations.
        /// </summary>
        private void RunBackup()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString) || !connectionString.Contains("Data Source="))
            {
                _logger.LogWarning("Backup service could not find a valid SQLite connection string.");
                return;
            }

            var dbPath = ResolvePath(GetDataSourcePath(connectionString));

            if (!File.Exists(dbPath))
            {
                _logger.LogWarning("Database file not found at {Path}, skipping backup.", dbPath);
                return;
            }

            var resolvedConnectionString = BuildConnectionStringWithDataSource(connectionString, dbPath);
            var configuredBackupPath = _configuration["DatabaseBackup:BackupPath"];
            var backupDir = string.IsNullOrWhiteSpace(configuredBackupPath)
                ? Path.Combine(_environment.ContentRootPath, "App_Data", "backups")
                : ResolvePath(configuredBackupPath);
            if (!Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }

            // Create one clean SQLite snapshot and reuse the same encrypted payload for each destination.
            CleanupStaleBackupStagingDirectories();
            var backupBytes = CreateSqliteBackupBytes(resolvedConnectionString);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
            var fileName = Path.GetFileNameWithoutExtension(dbPath);
            var extension = Path.GetExtension(dbPath);
            var backupFileName = $"{fileName}_{timestamp}_{Guid.NewGuid():N}{extension}.protected";

            CopyBackupToDirectory(backupBytes, backupDir, backupFileName, fileName, extension);

            foreach (var additionalBackupPath in GetAdditionalBackupPaths())
            {
                var additionalBackupDir = ResolvePath(additionalBackupPath);
                _logger.LogInformation("Additional database backup path resolved to: {Path}", additionalBackupDir);
                if (!Path.GetFullPath(additionalBackupDir).Equals(Path.GetFullPath(backupDir), StringComparison.OrdinalIgnoreCase))
                {
                    CopyBackupToDirectory(backupBytes, additionalBackupDir, backupFileName, fileName, extension);
                }
            }
        }

        /// <summary>
        /// Reads optional secondary backup locations from configuration.
        /// </summary>
        private IEnumerable<string> GetAdditionalBackupPaths()
        {
            var configuredPaths = new[]
            {
                _configuration["DatabaseBackup:AdditionalBackupPath"],
                _configuration["DatabaseBackup2:AdditionalBackupPath"]
            };

            return configuredPaths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path!)
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Encrypts and writes the backup file into one directory, then rotates old files.
        /// </summary>
        private void CopyBackupToDirectory(byte[] backupBytes, string backupDir, string backupFileName, string fileName, string extension)
        {
            Directory.CreateDirectory(backupDir);

            var backupPath = Path.Combine(backupDir, backupFileName);
            var temporaryProtectedPath = backupPath + $".tmp-{Guid.NewGuid():N}";
            var encryptedBackup = _backupProtector.Protect(backupBytes);

            try
            {
                // Write only ciphertext to the destination and publish it atomically.
                using (var stream = new FileStream(
                    temporaryProtectedPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    options: FileOptions.SequentialScan))
                {
                    stream.Write(encryptedBackup, 0, encryptedBackup.Length);
                    stream.Flush(flushToDisk: true);
                }

                // No overwrite: a collision is treated as a failed backup, never silent replacement.
                File.Move(temporaryProtectedPath, backupPath);
                _logger.LogInformation("Encrypted database backup created successfully: {Path}", backupPath);
            }
            finally
            {
                try
                {
                    if (File.Exists(temporaryProtectedPath))
                    {
                        File.Delete(temporaryProtectedPath);
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    _logger.LogWarning(ex, "Could not remove a temporary protected backup file.");
                }
            }

            RotateBackups(backupDir, fileName, extension);
        }

        /// <summary>
        /// Uses SQLite's backup API to create a consistent database snapshot in a temporary file.
        /// </summary>
        private byte[] CreateSqliteBackupBytes(string sourceConnectionString)
        {
            // Keep the plaintext SQLite snapshot on the local private application volume.
            // Never stage plaintext snapshots inside a configured secondary/off-host backup destination.
            var tempBackupDir = Path.Combine(
                _environment.ContentRootPath,
                "App_Data",
                "BackupStaging",
                Environment.MachineName,
                Environment.ProcessId.ToString());
            Directory.CreateDirectory(tempBackupDir);
            Directory.SetLastWriteTimeUtc(tempBackupDir, DateTime.UtcNow);

            var tempBackupPath = Path.Combine(
                tempBackupDir,
                $"IT_FormDB_backup_{Guid.NewGuid():N}.db");

            try
            {
                // The destination is a temporary SQLite database file that is deleted after bytes are read.
                var destinationConnectionString = new SqliteConnectionStringBuilder
                {
                    DataSource = tempBackupPath,
                    Mode = SqliteOpenMode.ReadWriteCreate,
                    Pooling = false
                }.ToString();

                using (var source = new SqliteConnection(sourceConnectionString))
                using (var destination = new SqliteConnection(destinationConnectionString))
                {
                    source.Open();
                    destination.Open();
                    source.BackupDatabase(destination);
                }

                return File.ReadAllBytes(tempBackupPath);
            }
            finally
            {
                try
                {
                    if (File.Exists(tempBackupPath))
                    {
                        File.Delete(tempBackupPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary SQLite backup snapshot {Path}.", tempBackupPath);
                }
            }
        }

        /// <summary>
        /// Removes abandoned staging directories from prior application processes.
        /// Only the dedicated backup-staging root is inspected.
        /// </summary>
        private void CleanupStaleBackupStagingDirectories()
        {
            var stagingRoot = Path.Combine(_environment.ContentRootPath, "App_Data", "BackupStaging");
            if (!Directory.Exists(stagingRoot))
            {
                return;
            }

            var currentProcessDirectory = Path.GetFullPath(Path.Combine(
                stagingRoot,
                Environment.MachineName,
                Environment.ProcessId.ToString()));
            var staleBeforeUtc = DateTime.UtcNow.AddHours(-24);

            foreach (var machineDirectory in Directory.EnumerateDirectories(stagingRoot))
            {
                foreach (var processDirectory in Directory.EnumerateDirectories(machineDirectory))
                {
                    var fullProcessDirectory = Path.GetFullPath(processDirectory);
                    if (fullProcessDirectory.Equals(currentProcessDirectory, StringComparison.OrdinalIgnoreCase)
                        || Directory.GetLastWriteTimeUtc(processDirectory) >= staleBeforeUtc)
                    {
                        continue;
                    }

                    try
                    {
                        Directory.Delete(processDirectory, recursive: true);
                        _logger.LogInformation("Removed stale plaintext backup staging directory for a stopped process.");
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        _logger.LogWarning(ex, "Could not remove a stale backup staging directory.");
                    }
                }

                try
                {
                    if (!Directory.EnumerateFileSystemEntries(machineDirectory).Any())
                    {
                        Directory.Delete(machineDirectory);
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    _logger.LogWarning(ex, "Could not remove an empty backup staging directory.");
                }
            }
        }

        /// <summary>
        /// Rebuilds the SQLite connection string with a resolved absolute DataSource path.
        /// </summary>
        private static string BuildConnectionStringWithDataSource(string connectionString, string dbPath)
        {
            var builder = new SqliteConnectionStringBuilder(connectionString)
            {
                DataSource = dbPath
            };

            return builder.ToString();
        }

        /// <summary>
        /// Keeps the newest ten backups in a destination directory and deletes older matching files.
        /// </summary>
        private void RotateBackups(string backupDir, string fileName, string extension)
        {
            var files = new DirectoryInfo(backupDir).GetFiles($"{fileName}_*{extension}")
                .Concat(new DirectoryInfo(backupDir).GetFiles($"{fileName}_*{extension}.protected"))
                .OrderByDescending(f => f.CreationTime)
                .Skip(10)
                .ToList();

            foreach (var file in files)
            {
                try
                {
                    file.Delete();
                    _logger.LogInformation("Deleted old backup: {Name}", file.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to delete old backup {Name}: {Msg}", file.Name, ex.Message);
                    // Rotation cleanup failure does not block future backup attempts.
                }
            }
        }

        /// <summary>
        /// Extracts the Data Source value from the configured SQLite connection string.
        /// </summary>
        private string GetDataSourcePath(string connectionString)
        {
            const string dataSourcePrefix = "Data Source=";
            var dataSourceStart = connectionString.IndexOf(dataSourcePrefix, StringComparison.OrdinalIgnoreCase) + dataSourcePrefix.Length;
            var separatorIndex = connectionString.IndexOf(';', dataSourceStart);
            var dataSourceEnd = separatorIndex >= 0 ? separatorIndex : connectionString.Length;

            return connectionString[dataSourceStart..dataSourceEnd].Trim();
        }

        /// <summary>
        /// Resolves relative paths and the |DataDirectory| token against the application content root.
        /// </summary>
        private string ResolvePath(string path)
        {
            const string dataDirectoryToken = "|DataDirectory|";
            if (path.Contains(dataDirectoryToken, StringComparison.OrdinalIgnoreCase))
            {
                // ASP.NET-style |DataDirectory| points at App_Data unless a custom value is registered.
                var configuredDataDirectory = AppDomain.CurrentDomain.GetData("DataDirectory") as string;
                var dataDirectory = string.IsNullOrWhiteSpace(configuredDataDirectory)
                    ? Path.Combine(_environment.ContentRootPath, "App_Data")
                    : configuredDataDirectory;

                var relativePath = path[(path.IndexOf(dataDirectoryToken, StringComparison.OrdinalIgnoreCase) + dataDirectoryToken.Length)..]
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                path = Path.Combine(dataDirectory, relativePath);
            }

            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(_environment.ContentRootPath, path);
            }

            return path;
        }

        /// <summary>
        /// Sends a background-service failure email to active users with the IT title.
        /// </summary>
        private async Task NotifyITAsync(string errorMessage)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IT_Form.Data.ApplicationDbContext>();
                var outlookService = scope.ServiceProvider.GetRequiredService<IT_Form.Services.IOutlookService>();

                // Notify only active IT users with email addresses configured.
                var itUsers = await dbContext.Users
                    .Where(u => u.IsActive && !string.IsNullOrEmpty(u.Email))
                    .Join(dbContext.UserTitles,
                        user => user.Id,
                        title => title.UserId,
                        (user, title) => new { User = user, Title = title })
                    .Where(x => x.Title.Title == "IT")
                    .Select(x => x.User.Email)
                    .Distinct()
                    .ToListAsync();

                if (!itUsers.Any())
                {
                    _logger.LogWarning("No active IT users found to notify about background service issue.");
                    return;
                }

                string toAddresses = string.Join(";", itUsers);
                string subject = "IT Form System Alert: Background Service Failure";
                string body = $@"
                    <div style='font-family: sans-serif; padding: 20px; border: 1px solid #f87171; border-radius: 8px; background: #fffafb;'>
                        <h2 style='color: #dc2626;'>System Alert</h2>
                        <p>There was an error executing a background service in the IT Form application.</p>
                        <p><b>Time:</b> {DateTime.UtcNow.AddHours(8):dd/MM/yyyy HH:mm:ss} (SGT)</p>
                        <div style='background: #fee2e2; padding: 15px; border-radius: 4px; border: 1px solid #fecaca; margin: 20px 0;'>
                            <p style='margin: 0; color: #991b1b; font-family: monospace;'>{errorMessage}</p>
                        </div>
                        <p>Please check the application server logs (app-.log) for the full stack trace.</p>
                        <hr style='border: 0; border-top: 1px solid #fecaca; margin: 20px 0;' />
                        <small style='color: #666;'>Automated System Monitoring</small>
                    </div>";

                outlookService.DisplayEmail(toAddresses, subject, body);
                _logger.LogInformation("Sent background service failure alert to {Count} IT staff.", itUsers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send background service failure alert.");
            }
        }
    }
}
