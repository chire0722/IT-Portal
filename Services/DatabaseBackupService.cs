using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Services
{
    public class DatabaseBackupService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseBackupService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly IServiceScopeFactory _scopeFactory;

        public DatabaseBackupService(
            IConfiguration configuration,
            ILogger<DatabaseBackupService> logger,
            IWebHostEnvironment environment,
            IServiceScopeFactory scopeFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _environment = environment;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // --- Initial Backup on Startup ---
            try
            {
                RunBackup();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to perform initial database backup.");
                await NotifyITAsync($"Initial startup database backup failed: {ex.Message}");
            }

            // --- Periodic Backup Loop (Targeting 2:00 AM daily) ---
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Calculate time until next 2:00 AM
                    var now = DateTime.Now;
                    var nextRunTime = now.Date.AddDays(1).AddHours(2); // Default to tomorrow at 2 AM
                    
                    // If it's already past 2 AM today, next run is tomorrow at 2 AM
                    // If it's before 2 AM today, next run is today at 2 AM
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
                    // Shutdown requested via stoppingToken
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Periodic daily database backup failed.");
                    await NotifyITAsync($"Periodic scheduled database backup failed: {ex.Message}");
                    
                    // On error, wait at least 1 hour before retrying to avoid spamming
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }

        private void RunBackup()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString) || !connectionString.Contains("Data Source="))
            {
                _logger.LogWarning("Backup service could not find a valid SQLite connection string.");
                return;
            }

            // Extract path from "Data Source=path/to/db"
            var dbPath = connectionString.Replace("Data Source=", "").Trim();

            // Resolve the legacy |DataDirectory| token before checking the file system.
            const string dataDirectoryToken = "|DataDirectory|";
            if (dbPath.Contains(dataDirectoryToken, StringComparison.OrdinalIgnoreCase))
            {
                var configuredDataDirectory = AppDomain.CurrentDomain.GetData("DataDirectory") as string;
                var dataDirectory = string.IsNullOrWhiteSpace(configuredDataDirectory)
                    ? Path.Combine(_environment.ContentRootPath, "App_Data")
                    : configuredDataDirectory;

                var relativeDbPath = dbPath[(dbPath.IndexOf(dataDirectoryToken, StringComparison.OrdinalIgnoreCase) + dataDirectoryToken.Length)..]
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                dbPath = Path.Combine(dataDirectory, relativeDbPath);
            }
            
            // If it's a relative path, combine with ContentRootPath
            if (!Path.IsPathRooted(dbPath))
            {
                dbPath = Path.Combine(_environment.ContentRootPath, dbPath);
            }

            if (!File.Exists(dbPath))
            {
                _logger.LogWarning("Database file not found at {Path}, skipping backup.", dbPath);
                return;
            }

            var backupDir = Path.Combine(_environment.ContentRootPath, "App_Data", "backups");
            if (!Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = Path.GetFileNameWithoutExtension(dbPath);
            var extension = Path.GetExtension(dbPath);
            var backupPath = Path.Combine(backupDir, $"{fileName}_{timestamp}{extension}");

            // Perform Copy
            File.Copy(dbPath, backupPath);
            _logger.LogInformation("Database backup created successfully: {Path}", backupPath);

            // Rotation logic: Keep last 10
            var files = new DirectoryInfo(backupDir).GetFiles($"*{extension}")
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
                    // Non-critical failure, no need to alert everyone
                }
            }
        }

        private async Task NotifyITAsync(string errorMessage)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IT_Form.Data.ApplicationDbContext>();
                var outlookService = scope.ServiceProvider.GetRequiredService<IT_Form.Services.IOutlookService>();

                // Get all active IT users
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
