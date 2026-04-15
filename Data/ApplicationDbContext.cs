using IT_Form.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text.Json;
using IT_Form.Attributes;
using IT_Form.Services;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Support;
using IT_Form.Helpers;
using IT_Form.Infrastructure.Notifications;

namespace IT_Form.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        private const string AuditCorrelationIdItemKey = "AuditCorrelationId";
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ApplicationDbContext> _logger;
        private readonly ILicenseEncryptionService _encryptionService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IPostCommitActionQueue _postCommitActionQueue;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options,
                                    IHttpContextAccessor httpContextAccessor,
                                    ILogger<ApplicationDbContext> logger,
                                    ILicenseEncryptionService encryptionService,
                                    IServiceProvider serviceProvider,
                                    IPostCommitActionQueue? postCommitActionQueue = null)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _encryptionService = encryptionService;
            _serviceProvider = serviceProvider;
            _postCommitActionQueue = postCommitActionQueue ?? NoOpPostCommitActionQueue.Instance;
        }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options,
                                    IHttpContextAccessor httpContextAccessor,
                                    ILogger<ApplicationDbContext> logger,
                                    ILicenseEncryptionService encryptionService)
            : this(
                options,
                httpContextAccessor,
                logger,
                encryptionService,
                new ServiceCollection().BuildServiceProvider(),
                null)
        {
        }

        public DbSet<OnboardingRequest> OnboardingRequests { get; set; } = null!;
        public DbSet<OffboardingRequest> OffboardingRequests { get; set; } = null!;
        public DbSet<SoftwareLicense> SoftwareLicenses { get; set; } = null!;
        public DbSet<SoftwareKey> SoftwareKeys { get; set; } = null!;
        public DbSet<IssuedSoftware> IssuedSoftware { get; set; } = null!;
        public DbSet<IssuedAsset> IssuedAssets { get; set; } = null!;
        public DbSet<FolderPermission> FolderPermissions { get; set; } = null!;
        public DbSet<OtpRecord> OtpRecords { get; set; } = null!;
        public DbSet<EmailOutboxMessage> EmailOutboxMessages { get; set; } = null!;
        public DbSet<UserTitle> UserTitles { get; set; } = null!;
        public DbSet<ITAsset> ITAssets { get; set; } = null!;
        public DbSet<MaintenanceLog> MaintenanceLogs { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<UserCompany> UserCompanies { get; set; } = null!;
        public DbSet<SigningToken> SigningTokens { get; set; } = null!;
        public DbSet<DeviceVerificationToken> DeviceVerificationTokens { get; set; } = null!;

        public DbSet<SlaSetting> SlaSettings { get; set; } = null!;
        public DbSet<SystemSetting> SystemSettings { get; set; } = null!;

        // Dynamic Dictionaries
        public DbSet<Company> Companies { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<StandardSoftware> StandardSoftware { get; set; } = null!;
        public DbSet<EmailGroup> EmailGroups { get; set; } = null!;
        public DbSet<SharedMailbox> SharedMailboxes { get; set; } = null!;
        public DbSet<ServerFolder> ServerFolders { get; set; } = null!;
        public DbSet<EmailContact> EmailContacts { get; set; } = null!;

        // Mapping Tables for Dictionaries
        public DbSet<RequestSoftware> RequestSoftware { get; set; } = null!;
        public DbSet<RequestEmailGroup> RequestEmailGroups { get; set; } = null!;
        public DbSet<RequestSharedMailbox> RequestSharedMailboxes { get; set; } = null!;
        public DbSet<DepartmentCompany> DepartmentCompanies { get; set; } = null!;
        public DbSet<EmailGroupCompany> EmailGroupCompanies { get; set; } = null!;
        public DbSet<SharedMailboxCompany> SharedMailboxCompanies { get; set; } = null!;
        public DbSet<ServerFolderCompany> ServerFolderCompanies { get; set; } = null!;
        public DbSet<SoftwareLicenseCompany> SoftwareLicenseCompanies { get; set; } = null!;

        internal string EncryptionModelCacheKey => _encryptionService.GetType().FullName ?? _encryptionService.GetType().Name;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ReplaceService<IModelCacheKeyFactory, ApplicationDbContextModelCacheKeyFactory>();
            base.OnConfiguring(optionsBuilder);
        }

        private static string EncryptWithFallback(string value, ILicenseEncryptionService encryptionService)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            var encrypted = encryptionService.EncryptKey(value);
            return string.IsNullOrEmpty(encrypted) ? value : encrypted;
        }

        private static string DecryptWithFallback(string value, ILicenseEncryptionService encryptionService)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            var decrypted = encryptionService.DecryptKey(value);
            return string.IsNullOrEmpty(decrypted) ? value : decrypted;
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // UserTitle -> ApplicationUser (many-to-one)
            builder.Entity<UserTitle>()
                .HasOne(ut => ut.User)
                .WithMany(u => u.UserTitles)
                .HasForeignKey(ut => ut.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // StandardSoftware -> SoftwareLicense (one-to-many, cascade delete)
            builder.Entity<SoftwareLicense>()
                .HasOne(sl => sl.StandardSoftware)
                .WithMany(ss => ss.Licenses)
                .HasForeignKey(sl => sl.StandardSoftwareId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique index: one title per user
            builder.Entity<UserTitle>()
                .HasIndex(ut => new { ut.UserId, ut.Title })
                .IsUnique();

            // UserCompany -> ApplicationUser (many-to-one)
            builder.Entity<UserCompany>()
                .HasOne(uc => uc.User)
                .WithMany(u => u.UserCompanies)
                .HasForeignKey(uc => uc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique index: one company per user
            builder.Entity<UserCompany>()
                .HasIndex(uc => new { uc.UserId, uc.CompanyName })
                .IsUnique();

            builder.Entity<SlaSetting>()
                .HasIndex(setting => setting.RequestType)
                .IsUnique();

            builder.Entity<Company>()
                .Property(company => company.TimeZoneId)
                .HasMaxLength(100)
                .HasDefaultValue(CompanyTimeZoneHelper.DefaultTimeZoneId);

            builder.Entity<Company>()
                .Property(company => company.BusinessStartTime)
                .HasDefaultValue(new TimeSpan(8, 30, 0));

            builder.Entity<Company>()
                .Property(company => company.BusinessEndTime)
                .HasDefaultValue(new TimeSpan(17, 30, 0));

            builder.Entity<Company>()
                .Property(company => company.WorksOnMonday)
                .HasDefaultValue(true);

            builder.Entity<Company>()
                .Property(company => company.WorksOnTuesday)
                .HasDefaultValue(true);

            builder.Entity<Company>()
                .Property(company => company.WorksOnWednesday)
                .HasDefaultValue(true);

            builder.Entity<Company>()
                .Property(company => company.WorksOnThursday)
                .HasDefaultValue(true);

            builder.Entity<Company>()
                .Property(company => company.WorksOnFriday)
                .HasDefaultValue(true);

            builder.Entity<Company>()
                .Property(company => company.WorksOnSaturday)
                .HasDefaultValue(true);

            builder.Entity<Company>()
                .Property(company => company.WorksOnSunday)
                .HasDefaultValue(false);

            builder.Entity<OnboardingRequest>()
                .HasIndex(request => new { request.Status, request.RequestDate });
            builder.Entity<OnboardingRequest>()
                .HasIndex(request => new { request.Status, request.UpdatedAt });
            builder.Entity<OnboardingRequest>()
                .HasIndex(request => new { request.Status, request.EmployeeSignedAt });
            builder.Entity<OnboardingRequest>()
                .HasIndex(request => new { request.RequestDate, request.Department });

            builder.Entity<OffboardingRequest>()
                .HasIndex(request => new { request.Status, request.RequestDate });
            builder.Entity<OffboardingRequest>()
                .HasIndex(request => new { request.Status, request.UpdatedAt });
            builder.Entity<OffboardingRequest>()
                .HasIndex(request => new { request.Status, request.EmployeeSignedAt });
            builder.Entity<OffboardingRequest>()
                .HasIndex(request => new { request.RequestDate, request.Department });

            builder.Entity<AuditLog>()
                .HasIndex(audit => new { audit.CreatedAt, audit.Timestamp });
            builder.Entity<AuditLog>()
                .HasIndex(audit => new { audit.EntityName, audit.IsSuccess, audit.CreatedAt });

            builder.Entity<ITAsset>()
                .HasIndex(asset => asset.WarrantyExpiry);
            builder.Entity<ITAsset>()
                .HasIndex(asset => asset.PurchaseDate);

            builder.Entity<SoftwareLicense>()
                .HasIndex(license => new { license.IsSubscription, license.ExpiryDate });

            // OnboardingRequest -> IssuedAssets (one-to-many)
            builder.Entity<IssuedAsset>()
                .HasOne(a => a.OnboardingRequest)
                .WithMany(r => r.IssuedAssets)
                .HasForeignKey(a => a.OnboardingRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            // OnboardingRequest -> FolderPermissions (one-to-many)
            builder.Entity<FolderPermission>()
                .HasOne(f => f.OnboardingRequest)
                .WithMany(r => r.FolderPermissions)
                .HasForeignKey(f => f.OnboardingRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            // OnboardingRequest -> IssuedSoftware (one-to-many)
            builder.Entity<IssuedSoftware>()
                .HasOne(s => s.OnboardingRequest)
                .WithMany(r => r.IssuedSoftware)
                .HasForeignKey(s => s.OnboardingRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- Dictionary Mapping Tables ---

            // RequestSoftware Mappings
            builder.Entity<RequestSoftware>()
                .HasOne(rs => rs.OnboardingRequest)
                .WithMany(r => r.RequestSoftware)
                .HasForeignKey(rs => rs.OnboardingRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            // RequestEmailGroup Mappings
            builder.Entity<RequestEmailGroup>()
                .HasOne(rg => rg.OnboardingRequest)
                .WithMany(r => r.RequestEmailGroups)
                .HasForeignKey(rg => rg.OnboardingRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            // RequestSharedMailbox Mappings
            builder.Entity<RequestSharedMailbox>()
                .HasOne(rm => rm.OnboardingRequest)
                .WithMany(r => r.RequestSharedMailboxes)
                .HasForeignKey(rm => rm.OnboardingRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- Multi-Company Mappings ---
            builder.Entity<DepartmentCompany>()
                .HasOne(dc => dc.Department)
                .WithMany(d => d.DepartmentCompanies)
                .HasForeignKey(dc => dc.DepartmentId);
            builder.Entity<DepartmentCompany>()
                .HasOne(dc => dc.Company)
                .WithMany()
                .HasForeignKey(dc => dc.CompanyId);

            builder.Entity<EmailGroupCompany>()
                .HasOne(ec => ec.EmailGroup)
                .WithMany(e => e.EmailGroupCompanies)
                .HasForeignKey(ec => ec.EmailGroupId);
            builder.Entity<EmailGroupCompany>()
                .HasOne(ec => ec.Company)
                .WithMany()
                .HasForeignKey(ec => ec.CompanyId);

            builder.Entity<SharedMailboxCompany>()
                .HasOne(sc => sc.SharedMailbox)
                .WithMany(s => s.SharedMailboxCompanies)
                .HasForeignKey(sc => sc.SharedMailboxId);
            builder.Entity<SharedMailboxCompany>()
                .HasOne(sc => sc.Company)
                .WithMany()
                .HasForeignKey(sc => sc.CompanyId);

            builder.Entity<ServerFolderCompany>()
                .HasOne(fc => fc.ServerFolder)
                .WithMany(s => s.ServerFolderCompanies)
                .HasForeignKey(fc => fc.ServerFolderId);
            builder.Entity<ServerFolderCompany>()
                .HasOne(fc => fc.Company)
                .WithMany()
                .HasForeignKey(fc => fc.CompanyId);

            builder.Entity<SoftwareLicenseCompany>()
                .HasOne(sc => sc.SoftwareLicense)
                .WithMany(license => license.SoftwareLicenseCompanies)
                .HasForeignKey(sc => sc.SoftwareLicenseId);
            builder.Entity<SoftwareLicenseCompany>()
                .HasOne(sc => sc.Company)
                .WithMany()
                .HasForeignKey(sc => sc.CompanyId);

            // Index for OTP lookups
            builder.Entity<OtpRecord>()
                .HasIndex(o => new { o.Email, o.Code });

            builder.Entity<EmailOutboxMessage>()
                .HasIndex(message => new
                {
                    message.Status,
                    message.AvailableAfterUtc,
                    message.LockedUntilUtc
                });

            // ITAsset -> IssuedAssets (one-to-many)
            builder.Entity<IssuedAsset>()
                .HasOne(a => a.ITAsset)
                .WithMany(i => i.IssuedHistory)
                .HasForeignKey(a => a.ITAssetId)
                .OnDelete(DeleteBehavior.SetNull);

            // ITAsset -> MaintenanceLogs (one-to-many)
            builder.Entity<MaintenanceLog>()
                .HasOne(m => m.ITAsset)
                .WithMany(i => i.MaintenanceLogs)
                .HasForeignKey(m => m.ITAssetId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure MaintenanceLog Cost precision
            builder.Entity<MaintenanceLog>()
                .Property(m => m.Cost)
                .HasPrecision(18, 2);

            // SoftwareLicense -> SoftwareKeys (one-to-many)
            builder.Entity<SoftwareKey>()
                .HasOne(k => k.SoftwareLicense)
                .WithMany(l => l.Keys)
                .HasForeignKey(k => k.SoftwareLicenseId)
                .OnDelete(DeleteBehavior.Cascade);

            // OnboardingRequest -> SoftwareKeys (one-to-many / assignment)
            builder.Entity<SoftwareKey>()
                .HasOne(k => k.OnboardingRequest)
                .WithMany()
                .HasForeignKey(k => k.OnboardingRequestId)
                .OnDelete(DeleteBehavior.SetNull);

            // Unique KeyValueHash for idempotency
            builder.Entity<SoftwareKey>()
                .HasIndex(k => k.KeyValueHash)
                .IsUnique()
                .HasFilter("[KeyValueHash] IS NOT NULL");

            // Unique AssetTag for active ITAsset rows only (soft-deleted rows can reuse tags)
            builder.Entity<ITAsset>()
                .HasIndex(a => a.AssetTag)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            // DeviceVerificationToken -> ApplicationUser
            builder.Entity<DeviceVerificationToken>()
                .HasOne(t => t.Employee)
                .WithMany()
                .HasForeignKey(t => t.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);

            // Seed default SLA Settings
            var defaultDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            builder.Entity<SlaSetting>().HasData(
                new SlaSetting { Id = 1, RequestType = "Onboarding", TargetDays = 3, EscalationDaysTier1 = 3, EscalationDaysTier2 = 7, UpdatedAt = defaultDate },
                new SlaSetting { Id = 2, RequestType = "Offboarding", TargetDays = 3, EscalationDaysTier1 = 3, EscalationDaysTier2 = 7, UpdatedAt = defaultDate }
            );

            // Seed default System Settings
            builder.Entity<SystemSetting>().HasData(
                new SystemSetting 
                { 
                    Key = "AuditNotificationEmail", 
                    Value = "itdept@ytcprecast.com;tayziheng@yengtong.com", 
                    Description = "Email address to receive notifications for Create/Update audit events." 
                }
            );

            // Value Converters for [EncryptColumn]
            var encryptionService = _encryptionService;
            var encryptionConverter = new ValueConverter<string, string>(
                v => EncryptWithFallback(v, encryptionService),
                v => DecryptWithFallback(v, encryptionService)
            );

            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                var properties = entityType.ClrType.GetProperties()
                    .Where(p => p.GetCustomAttribute<EncryptColumnAttribute>() != null);

                foreach (var property in properties)
                {
                    builder.Entity(entityType.ClrType)
                        .Property(property.Name)
                        .HasConversion(encryptionConverter)
                        .HasMaxLength(500); // Override length to accommodate Base64 inflation
                }
            }

            // Global Query Filters for Soft Deletes
            builder.Entity<StandardSoftware>().HasQueryFilter(e => !e.IsDeleted);
            builder.Entity<SoftwareLicense>().HasQueryFilter(e => !e.IsDeleted);
            builder.Entity<SoftwareKey>().HasQueryFilter(e => !e.IsDeleted);
            builder.Entity<ITAsset>().HasQueryFilter(e => !e.IsDeleted);
            builder.Entity<MaintenanceLog>().HasQueryFilter(e => !e.IsDeleted);

            // Cascade Query Filters to satisfy EF Core required relationship warnings
            builder.Entity<RequestSoftware>().HasQueryFilter(e => !e.StandardSoftware!.IsDeleted);
            builder.Entity<IssuedSoftware>().HasQueryFilter(e => !e.SoftwareLicense.IsDeleted);
            builder.Entity<SoftwareLicenseCompany>().HasQueryFilter(e => !e.SoftwareLicense.IsDeleted);
            builder.Entity<MaintenanceLog>().HasQueryFilter(e => !e.ITAsset!.IsDeleted);
        }

        // Entities we want to audit
        private static readonly HashSet<string> AuditedEntities = new()
        {
            nameof(ApplicationUser),
            nameof(OnboardingRequest),
            nameof(OffboardingRequest),
            nameof(SoftwareLicense),
            nameof(ITAsset),
            nameof(MaintenanceLog),
            nameof(SoftwareKey),
            nameof(DeviceVerificationToken),
            nameof(SlaSetting),
            nameof(Company),
            nameof(Department),
            nameof(StandardSoftware),
            nameof(EmailGroup),
            nameof(SharedMailbox),
            nameof(ServerFolder),
            nameof(EmailContact),
            nameof(UserTitle),
            nameof(UserCompany),
            // HR Mapping Entities
            nameof(RequestSoftware),
            nameof(RequestEmailGroup),
            nameof(RequestSharedMailbox),
            nameof(DepartmentCompany),
            nameof(EmailGroupCompany),
            nameof(SharedMailboxCompany),
            nameof(ServerFolderCompany),
            nameof(IssuedAsset),
            nameof(IssuedSoftware),
            nameof(FolderPermission)
        };

        // Re-entry guard — prevents the audit-log save from triggering another audit pass
        private bool _saving = false;

        // NOTE: All controller code uses SaveChangesAsync — audit logging happens there.
        // The sync overrides below call base.SaveChanges() directly to avoid a
        // sync-over-async deadlock (calling .GetAwaiter().GetResult() on an async
        // method inside ASP.NET is a known deadlock risk). Audit is intentionally
        // skipped on the sync path since it should never be used in normal app flow.
        public override int SaveChanges()
        {
            return base.SaveChanges();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public async Task LogActionAsync(string entityName, string entityId, string action, string details, string? changes = null)
        {
            var currentUser = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "System";
            var ipAddress = _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString();

            var log = new AuditLog
            {
                UserId = currentUser,
                EntityName = entityName,
                EntityId = entityId,
                Action = action,
                PerformedBy = currentUser,
                Timestamp = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                Details = details,
                Changes = changes,
                Payload = changes,
                IsSuccess = true,
                ErrorMessage = null,
                IpAddress = ipAddress,
                CorrelationId = GetCurrentCorrelationId()
            };

            AuditLogs.Add(log);
            await base.SaveChangesAsync();
        }

        public async Task PersistStandaloneAuditLogAsync(AuditLog log, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(log);

            if (!Database.IsRelational())
            {
                AuditLogs.Add(log);
                await base.SaveChangesAsync(cancellationToken);
                return;
            }

            await Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO AuditLogs
                    (UserId, Timestamp, CreatedAt, EntityName, EntityId, Action, PerformedBy, ActorName, Details, Changes, Payload, IsSuccess, ErrorMessage, IpAddress, CorrelationId)
                VALUES
                    ({log.UserId}, {log.Timestamp}, {log.CreatedAt}, {log.EntityName}, {log.EntityId}, {log.Action}, {log.PerformedBy}, {log.ActorName}, {log.Details}, {log.Changes}, {log.Payload}, {log.IsSuccess}, {log.ErrorMessage}, {log.IpAddress}, {log.CorrelationId})",
                cancellationToken);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // If we are already inside a save (writing audit logs), skip audit pass
            if (_saving)
                return await base.SaveChangesAsync(cancellationToken);

            ChangeTracker.DetectChanges();

            var currentUser = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "System";
            var currentUserId = _httpContextAccessor?.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var request = _httpContextAccessor?.HttpContext?.Request;
            string? currentBaseUrl = request != null ? $"{request.Scheme}://{request.Host}{request.PathBase}" : null;
            var auditEntries = BuildAuditEntries(currentUser);

            // Process Soft Deletes
            foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
            {
                if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = DateTime.UtcNow;
                    entry.Entity.DeletedByUserId = currentUserId; // Can be user ID, mapped if needed
                }
            }

            _saving = true;
            var isInMemory = Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
            var transaction = (Database.CurrentTransaction == null && !isInMemory) ? await Database.BeginTransactionAsync(cancellationToken) : null;
            int result;
            try
            {
                result = await base.SaveChangesAsync(cancellationToken);

                // For Added entries the DB-generated PK is now available
                if (auditEntries.Count > 0)
                {
                    foreach (var (entry, audit) in auditEntries.Where(x => x.audit.Action == "Created"))
                    {
                        var keyProp = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
                        if (keyProp != null)
                        {
                            audit.EntityId = keyProp.CurrentValue?.ToString() ?? "?";
                        }
                    }

                    AuditLogs.AddRange(auditEntries.Select(x => x.audit));
                    await base.SaveChangesAsync(cancellationToken);

                    EnqueueAuditNotifications(auditEntries.Select(x => x.audit).ToList(), currentBaseUrl);
                }

                if (transaction != null)
                {
                    await transaction.CommitAsync(cancellationToken);
                    await _postCommitActionQueue.FlushAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                // Log via the structured logger instead of a raw text file
                var msg = ex.InnerException?.Message ?? ex.Message;
                _logger.LogError(ex, "Failed to save audit log entries: {Message}", msg);
                _postCommitActionQueue.Clear();
                if (transaction != null)
                    await transaction.RollbackAsync(cancellationToken);
                throw;
            }
            finally
            {
                if (transaction != null)
                    await transaction.DisposeAsync();
                _saving = false;
            }

            return result;
        }

        private void EnqueueAuditNotifications(List<AuditLog> logsForEmail, string? currentBaseUrl)
        {
            if (logsForEmail.Count == 0)
            {
                return;
            }

            _postCommitActionQueue.Enqueue(ct => DispatchAuditNotificationsAfterCommitAsync(logsForEmail, currentBaseUrl, ct));
        }

        private async Task DispatchAuditNotificationsAfterCommitAsync(
            List<AuditLog> logsForEmail,
            string? currentBaseUrl,
            CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var scopedContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await scopedContext.SendAuditNotificationsAsync(logsForEmail, currentBaseUrl, cancellationToken);
            }
            catch (Exception ex) when (!CriticalExceptionDetector.IsCritical(ex))
            {
                _logger.LogError(ex, "Audit notification dispatch failed after commit.");
            }
        }

        private List<(EntityEntry entry, AuditLog audit)> BuildAuditEntries(string currentUser)
        {
            if (AuditedEntities == null) return new List<(EntityEntry, AuditLog)>();
            ChangeTracker.DetectChanges();
            var list = new List<(EntityEntry, AuditLog)>();

            foreach (var entry in ChangeTracker.Entries())
            {
                var entityType = entry.Entity.GetType().Name;
                if (!AuditedEntities.Contains(entityType)) continue;
                if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged) continue;

                var action = entry.State switch
                {
                    EntityState.Added => "Created",
                    EntityState.Modified => "Updated",
                    EntityState.Deleted => "Deleted",
                    _ => null
                };
                if (action == null) continue;

                if (action == "Updated")
                {
                    var statusProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Status");
                    if (statusProp != null && statusProp.IsModified && statusProp.CurrentValue?.ToString() == "Rejected")
                    {
                        action = "Rejected";
                    }
                }

                var key = entry.Properties
                    .FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "0";

                // Build a human-readable diff
                var changes = new Dictionary<string, object?>();
                foreach (var prop in entry.Properties)
                {
                    // Skip Base64 signature blobs to keep log readable
                    var propName = prop.Metadata.Name;
                    if (propName.Contains("Signature", StringComparison.OrdinalIgnoreCase))
                    {
                        if (prop.IsModified)
                            changes[propName] = prop.CurrentValue is null ? null : "<signature updated>";
                        continue;
                    }

                    // Mask encrypted PII columns
                    bool isEncrypted = entry.Entity.GetType().GetProperty(propName)?.GetCustomAttribute<EncryptColumnAttribute>() != null;

                    if (entry.State == EntityState.Added)
                    {
                        changes[propName] = isEncrypted ? "***ENCRYPTED PII***" : prop.CurrentValue;
                    }
                    else if (entry.State == EntityState.Modified && prop.IsModified)
                    {
                        changes[propName] = isEncrypted
                            ? new { From = "***ENCRYPTED PII***", To = "***ENCRYPTED PII***" }
                            : new { From = prop.OriginalValue, To = prop.CurrentValue };
                    }
                    else if (entry.State == EntityState.Deleted)
                    {
                        changes[propName] = isEncrypted ? "***ENCRYPTED PII***" : prop.OriginalValue;
                    }
                }

                // Build a human-readable description (Details)
                string? details = null;
                try
                {
                    details = entityType switch
                    {
                        nameof(ITAsset) => $"Asset: {entry.Property("AssetTag").CurrentValue ?? entry.Property("AssetTag").OriginalValue} ({(entry.Property("Model").CurrentValue ?? entry.Property("Model").OriginalValue)})",
                        nameof(SoftwareLicense) => $"Software: {entry.Property("SoftwareName").CurrentValue ?? entry.Property("SoftwareName").OriginalValue}",
                        nameof(OnboardingRequest) => $"Onboarding: {entry.Property("FullName").CurrentValue ?? entry.Property("FullName").OriginalValue} ({entry.Property("Department").CurrentValue ?? entry.Property("Department").OriginalValue})",
                        nameof(OffboardingRequest) => $"Offboarding: {entry.Property("FullName").CurrentValue ?? entry.Property("FullName").OriginalValue} ({entry.Property("Department").CurrentValue ?? entry.Property("Department").OriginalValue})",
                        nameof(MaintenanceLog) => $"Maintenance: {entry.Property("Description").CurrentValue ?? entry.Property("Description").OriginalValue}",
                        nameof(Company) => $"Company: {entry.Property("Name").CurrentValue ?? entry.Property("Name").OriginalValue}",
                        nameof(Department) => $"Department: {entry.Property("Name").CurrentValue ?? entry.Property("Name").OriginalValue}",
                        nameof(StandardSoftware) => $"Software Type: {entry.Property("Name").CurrentValue ?? entry.Property("Name").OriginalValue}",
                        nameof(EmailGroup) => $"Email Group: {entry.Property("GroupName").CurrentValue ?? entry.Property("GroupName").OriginalValue}",
                        nameof(SharedMailbox) => $"Shared Mailbox: {entry.Property("MailboxName").CurrentValue ?? entry.Property("MailboxName").OriginalValue}",
                        nameof(ServerFolder) => $"Server Folder: {entry.Property("FolderName").CurrentValue ?? entry.Property("FolderName").OriginalValue}",
                        nameof(EmailContact) => $"Contact: {entry.Property("Name").CurrentValue ?? entry.Property("Name").OriginalValue} ({entry.Property("Email").CurrentValue ?? entry.Property("Email").OriginalValue})",
                        nameof(UserTitle) => $"User Role: {entry.Property("Title").CurrentValue ?? entry.Property("Title").OriginalValue} for {entry.Property("UserId").CurrentValue ?? entry.Property("UserId").OriginalValue}",
                        nameof(UserCompany) => $"User Branch: {entry.Property("CompanyName").CurrentValue ?? entry.Property("CompanyName").OriginalValue} for {entry.Property("UserId").CurrentValue ?? entry.Property("UserId").OriginalValue}",
                        nameof(ApplicationUser) => $"Account: {entry.Property("FullName").CurrentValue ?? entry.Property("FullName").OriginalValue} ({entry.Property("UserName").CurrentValue ?? entry.Property("UserName").OriginalValue})",
                        nameof(SoftwareKey) => $"Software Key ID: {key}",
                        nameof(RequestSoftware) => $"Mapped Software: {entry.Property("SoftwareName").CurrentValue ?? entry.Property("SoftwareName").OriginalValue}",
                        nameof(RequestEmailGroup) => $"Mapped Email Group: {entry.Property("GroupName").CurrentValue ?? entry.Property("GroupName").OriginalValue}",
                        nameof(RequestSharedMailbox) => $"Mapped Shared Mailbox: {entry.Property("MailboxName").CurrentValue ?? entry.Property("MailboxName").OriginalValue}",
                        nameof(IssuedAsset) => $"Issued Asset: {entry.Property("AssetTag").CurrentValue ?? entry.Property("AssetTag").OriginalValue} ({(entry.Property("AssetType").CurrentValue ?? entry.Property("AssetType").OriginalValue)})",
                        nameof(IssuedSoftware) => $"Issued Software: {entry.Property("SoftwareName").CurrentValue ?? entry.Property("SoftwareName").OriginalValue}",
                        nameof(FolderPermission) => $"Folder Permission: {entry.Property("FolderName").CurrentValue ?? entry.Property("FolderName").OriginalValue}",
                        nameof(SlaSetting) => $"SLA Setting: {entry.Property("RequestType").CurrentValue ?? entry.Property("RequestType").OriginalValue}",
                        _ => null
                    };
                }
                catch { /* Ignore if property doesn't exist */ }

                // Use universal formatter for consistent non-technical language
                var changesText = IT_Form.Helpers.AuditFormatter.FormatChanges(null); // Default
                if (action == "Created")
                {
                    changesText = "Record created and initialized.";
                }
                else if (action == "Updated")
                {
                    // Convert the dictionary to a temporary JSON string to use the universal formatter logic
                    var tempJson = JsonSerializer.Serialize(changes);
                    changesText = IT_Form.Helpers.AuditFormatter.FormatChanges(tempJson);
                }
                else if (action == "Deleted")
                {
                    // For deletions, format the original values to show what was lost
                    var tempJson = JsonSerializer.Serialize(changes);
                    changesText = "Deleted Data: " + IT_Form.Helpers.AuditFormatter.FormatChanges(tempJson);
                }

                list.Add((entry, new AuditLog
                {
                    UserId = currentUser,
                    EntityName = entityType,
                    EntityId = key,
                    Action = action,
                    PerformedBy = currentUser,
                    Timestamp = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    Details = details,
                    Changes = JsonSerializer.Serialize(changes),
                    Payload = JsonSerializer.Serialize(changes),
                    IsSuccess = true,
                    ErrorMessage = null,
                    CorrelationId = GetCurrentCorrelationId()
                }));
            }

            return list;
        }

        private Guid? GetCurrentCorrelationId()
        {
            var item = _httpContextAccessor?.HttpContext?.Items[AuditCorrelationIdItemKey];
            return item is Guid correlationId ? correlationId : null;
        }

        internal async Task SendAuditNotificationsAsync(
            List<AuditLog> entries,
            string? baseUrl = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Use a separate scope to avoid DbContext state issues if needed, 
                // but here we just need the setting value.
                var auditEmail = await SystemSettings
                    .Where(s => s.Key == "AuditNotificationEmail")
                    .Select(s => s.Value)
                    .FirstOrDefaultAsync(cancellationToken);

                if (string.IsNullOrEmpty(auditEmail)) return;
                
                var queuedMessages = new List<EmailOutboxMessage>();

                foreach (var log in entries.Where(l => 
                    (l.EntityName == "OnboardingRequest" || l.EntityName == "OffboardingRequest") &&
                    (l.Action == "Rejected" || l.Action == "Deleted" || IsSigningEvent(l))))
                {

                    var subject = $"AUDIT ALERT: {log.Action} - {log.EntityName} (#{log.EntityId})";
                    var greeting = "Hello,";
                    
                    // Format the raw JSON back into human-friendly text for the email
                    var formattedChanges = !string.IsNullOrEmpty(log.Changes) 
                        ? IT_Form.Helpers.AuditFormatter.FormatChanges(log.Changes) 
                        : "No data changes recorded.";

                    var paramsRows = $@"
                        <tr>
                            <td style='padding: 14px 15px; font-size: 14px; color: #1E293B; border-bottom: 1px solid #F1F5F9; font-weight: 600;'>Action</td>
                            <td style='padding: 14px 15px; font-size: 14px; color: {(log.Action == "Created" ? "#10B981" : "#F59E0B")}; border-bottom: 1px solid #F1F5F9; font-weight: 700;'>{log.Action.ToUpper()}</td>
                        </tr>
                        <tr style='background-color: #F8FAFC;'>
                            <td style='padding: 14px 15px; font-size: 14px; color: #1E293B; border-bottom: 1px solid #F1F5F9; font-weight: 600;'>Entity</td>
                            <td style='padding: 14px 15px; font-size: 14px; color: #475569; border-bottom: 1px solid #F1F5F9;'>{log.EntityName} (#{log.EntityId})</td>
                        </tr>
                        <tr>
                            <td style='padding: 14px 15px; font-size: 14px; color: #1E293B; border-bottom: 1px solid #F1F5F9; font-weight: 600;'>Performed By</td>
                            <td style='padding: 14px 15px; font-size: 14px; color: #475569; border-bottom: 1px solid #F1F5F9;'>{log.PerformedBy}</td>
                        </tr>
                        <tr style='background-color: #F8FAFC;'>
                            <td style='padding: 14px 15px; font-size: 14px; color: #1E293B; border-bottom: 1px solid #F1F5F9; font-weight: 600;'>Timestamp</td>
                            <td style='padding: 14px 15px; font-size: 14px; color: #475569; border-bottom: 1px solid #F1F5F9;'>{log.Timestamp.ToLocalTime():dd MMM yyyy, hh:mm tt}</td>
                        </tr>
                        <tr>
                            <td style='padding: 14px 15px; font-size: 14px; color: #1E293B; border-bottom: 1px solid #F1F5F9; font-weight: 600;'>Details</td>
                            <td style='padding: 14px 15px; font-size: 14px; color: #475569; border-bottom: 1px solid #F1F5F9;'>{log.Details}</td>
                        </tr>
                        <tr style='background-color: #F8FAFC;'>
                            <td colspan='2' style='padding: 15px; font-size: 13px; color: #475569;'>
                                <div style='font-weight: 700; color: #64748B; text-transform: uppercase; font-size: 11px; margin-bottom: 8px;'>Changes:</div>
                                <div style='white-space: pre-wrap; font-family: monospace; background: #FFF; padding: 10px; border: 1px solid #E2E8F0; border-radius: 4px;'>{formattedChanges}</div>
                            </td>
                        </tr>";
                    
                    var config = _serviceProvider.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
                    string appDomainSetting = config?["AppDomain"] ?? "";
                    var domains = appDomainSetting.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    string fallbackDomain = domains.FirstOrDefault(d => !d.Contains("localhost")) ?? domains.FirstOrDefault() ?? "http://localhost:5063";
                    
                    // Priority: Explicitly passed baseUrl > AppDomain config > localhost
                    string appDomain = baseUrl ?? fallbackDomain;
                    
                    var modulePath = log.EntityName switch {
                        "OnboardingRequest" => "Onboarding",
                        "OffboardingRequest" => "Offboarding",
                        "ITAsset" => "ITAsset",
                        "SoftwareLicense" => "SoftwareLicense",
                        _ => "AuditLog"
                    };
                    var detailsUrl = $"{appDomain}/{modulePath}/Details/{log.EntityId}";

                    var htmlBody = IT_Form.Helpers.EmailTemplateHelper.GetProfessionalHtmlTemplate(
                        "Audit Notification",
                        $"{greeting}<br/><br/>An administrative action has been captured in the system audit log that requires your awareness.",
                        paramsRows,
                        detailsUrl,
                        "View Audit Record"
                    );

                    queuedMessages.Add(EmailOutboxMessageSerializer.Serialize(
                        new RenderedEmailMessage(
                            auditEmail,
                            subject,
                            htmlBody,
                            BypassActiveCheck: true)));
                }

                if (queuedMessages.Count > 0)
                {
                    EmailOutboxMessages.AddRange(queuedMessages);
                    await SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in SendAuditNotificationsAsync");
            }
        }
        // Known signature field names that indicate an actual signing action
        private static readonly HashSet<string> _signatureFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "RequesterSignedAt", "HRSignedAt", "HODSignedAt",
            "ITAdminSignedAt",   "EmployeeSignedAt", "PdpaConsentedAt"
        };

        /// <summary>
        /// Returns true ONLY when a recognized signature field transitions from null → a timestamp.
        /// Any other field change (e.g. EmailForwarded, ChecklistRemarks) will return false.
        /// </summary>
        private bool IsSigningEvent(AuditLog log)
        {
            if (string.IsNullOrEmpty(log.Changes)) return false;

            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(log.Changes);
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    // Only care about known signature fields
                    if (!_signatureFields.Contains(prop.Name)) continue;

                    // The value must be an object { From: null, To: "<timestamp>" }
                    if (prop.Value.ValueKind != System.Text.Json.JsonValueKind.Object) continue;

                    if (prop.Value.TryGetProperty("From", out var from) &&
                        prop.Value.TryGetProperty("To",   out var to))
                    {
                        // Signature was added (null → value)
                        if (from.ValueKind == System.Text.Json.JsonValueKind.Null &&
                            to.ValueKind   != System.Text.Json.JsonValueKind.Null)
                        {
                            return true;
                        }
                    }
                }
            }
            catch { /* Malformed JSON – treat as non-signing */ }

            return false;
        }
    }
}

