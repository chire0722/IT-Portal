// Project comment: Program.cs is part of the IT Form application source code.

using IT_Form.Data;
using IT_Form.Models;
using IT_Form.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using Serilog;
using Serilog.Events;
using IT_Form.Models.Enums;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using IT_Form.Security;
using IT_Form.Application.Common.Support.Internal.Runtime.ControlPlane.Operations;
using IT_Form.Application.Common.Support.Internal.Runtime.ControlPlane.Policies;
using IT_Form.Helpers;
using IT_Form.Filters;
using IT_Form.Application.Common.Abstractions;
using IT_Form.Application.Common.Behaviors;
using IT_Form.Application.Common.Mapping;
using IT_Form.Application.Common.Support;
using IT_Form.Application.Features.Account.Queries.GetLoginPageBranding;
using IT_Form.Infrastructure.Notifications;
using Mapster;
using MapsterMapper;
using System.Net.NetworkInformation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using FluentValidation;
using MediatR;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using System.Net;



QuestPDF.Settings.License = LicenseType.Community;
const string AppListenUrl = "http://0.0.0.0:5063";

// Diagnostic Check
if (args.Contains("--seed-check"))
{
    var tempBuilder = WebApplication.CreateBuilder(args);
    var tempDataDirectory = Path.Combine(tempBuilder.Environment.ContentRootPath, "App_Data");
    Directory.CreateDirectory(tempDataDirectory);
    AppDomain.CurrentDomain.SetData("DataDirectory", tempDataDirectory);

    tempBuilder.Services.AddLogging();
    tempBuilder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(
            ResolveSqliteConnectionString(
                tempBuilder.Configuration.GetConnectionString("DefaultConnection"),
                tempBuilder.Environment.ContentRootPath,
                tempDataDirectory),
            o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
    tempBuilder.Services.AddHttpContextAccessor();
    var tempApp = tempBuilder.Build();
    using (var scope = tempApp.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Console.WriteLine($"ITAssets: {db.ITAssets.Count()}");
        Console.WriteLine($"SoftwareKeys: {db.SoftwareKeys.Count()}");
        Console.WriteLine($"OnboardingRequests: {db.OnboardingRequests.Count()}");
        Console.WriteLine($"AuditLogs: {db.AuditLogs.Count()}");
        Console.WriteLine($"SoftwareLicenses: {db.SoftwareLicenses.Count()}");
    }
    return;
}


// --- Serilog: Configure rolling file logger before host is built ---
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.DataProtection", LogEventLevel.Error)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/app-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,           // Keep last 30 days of logs
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

if (StartupUrlGuard.TryGetConflictingUrls(AppListenUrl, out var conflictingUrls))
{
    Log.Error(
        "Startup aborted because the configured URL(s) are already in use: {Urls}. Stop the existing app instance or change the configured port.",
        string.Join(", ", conflictingUrls));
    Environment.ExitCode = 1;
    return;
}

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables(prefix: "IT_FORM__");
builder.WebHost.UseUrls(AppListenUrl);

MapsterConfig.RegisterMappings();

var cookieSecurePolicy = ResolveCookieSecurePolicy(builder.Environment, builder.Configuration);

// --- Kestrel: Fingerprint Masking ---
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    // The LAN deployment must listen on all interfaces, so the binding is set explicitly above.
});

// Use Serilog as the logging provider
builder.Host.UseSerilog();

// --- Set DataDirectory for LocalDB/SQLite creation ---
string dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
if (!Directory.Exists(dataDirectory))
{
    Directory.CreateDirectory(dataDirectory);
}
AppDomain.CurrentDomain.SetData("DataDirectory", dataDirectory);

// --- Data Protection ---
// Persist keys outside the publish folder so cookies remain valid across republishes on the same machine.
var localAppDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
var dataProtectionRoot = string.IsNullOrWhiteSpace(localAppDataRoot)
    ? dataDirectory
    : Path.Combine(localAppDataRoot, "IT_Form");
var dataProtectionPath = Path.Combine(dataProtectionRoot, "DataProtection-Keys");
Directory.CreateDirectory(dataProtectionPath);
var dataProtectionBuilder = builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("IT_Form");



// --- Database ---
var resolvedDefaultConnectionString = ResolveSqliteConnectionString(
    builder.Configuration.GetConnectionString("DefaultConnection"),
    builder.Environment.ContentRootPath,
    dataDirectory);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(resolvedDefaultConnectionString,
        o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));

    // SQLite table rebuild migrations emit a one-time non-transactional warning
    // when EF toggles foreign key enforcement during schema changes.
    options.ConfigureWarnings(warnings =>
        warnings
            .Ignore(RelationalEventId.NonTransactionalMigrationOperationWarning)
            .Ignore(CoreEventId.FirstWithoutOrderByAndFilterWarning));
});

// Required for ApplicationDbContext to capture the current user for audit logs
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<LoginPageBrandingOptions>(builder.Configuration.GetSection("LoginPageBranding"));
builder.Services.AddMemoryCache();

// --- Identity ---

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // --- Strengthened Password Policy (NIST SP 800-63B aligned) ---
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 10;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequiredUniqueChars = 4;
    options.SignIn.RequireConfirmedEmail = false;
    options.User.RequireUniqueEmail = true;
    // Identity lockout defaults; the login handler applies the configured escalating lockout schedule.
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// --- Cookie Config ---
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    // Direct unauthorized users to a proper 403 page instead of the login screen
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(4);          // Reduced from 8h to 4h
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;           // SECURITY: Prevent CSRF via top-level navigation
    options.Cookie.Name = "YTC.IT.Auth.v2";                  // Rotate cookie name to avoid reading stale auth cookies
    options.Cookie.SecurePolicy = cookieSecurePolicy;
});

// Hardened: Global Forced Logout - check security stamp on EVERY request
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
});

// --- Session (for wizard state) ---
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;           // SECURITY: Prevent session CSRF
    options.Cookie.Name = "YTC.IT.Session.v2";               // Rotate cookie name to avoid reading stale session cookies
    options.Cookie.SecurePolicy = cookieSecurePolicy;
});

// --- Services ---
// builder.Services.AddScoped<IEmailService, EmailService>(); // REMOVED: SMTP disabled
builder.Services.AddScoped<IEmailOtpService, EmailOtpService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<ICompanyReferenceSyncService, CompanyReferenceSyncService>();
builder.Services.AddScoped<IOnboardingWizardService, OnboardingWizardService>();
builder.Services.AddScoped<IOnboardingDocumentService, OnboardingDocumentService>();
builder.Services.AddScoped<IOnboardingSessionAccessor, OnboardingSessionAccessor>();
builder.Services.AddScoped<ICurrentEmployeeAccessService, CurrentEmployeeAccessService>();
builder.Services.AddScoped<IOnboardingHodService, OnboardingHodService>();
builder.Services.AddScoped<LegacySensitiveUploadMigrationService>();
builder.Services.AddScoped<AuditLogRepairService>();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
builder.Services.AddScoped<IQuotaControl, QuotaControl>();
builder.Services.AddScoped<ISystemHealthService, SystemHealthService>();
builder.Services.AddScoped<IBackupManagementService, BackupManagementService>();
builder.Services.AddScoped<IMaintenanceModeService, MaintenanceModeService>();
builder.Services.AddScoped<ISecurityCenterService, SecurityCenterService>();
builder.Services.AddScoped<IDataCleanupPreviewService, DataCleanupPreviewService>();
builder.Services.AddScoped<ISafeSystemSettingsService, SafeSystemSettingsService>();
builder.Services.AddSingleton<ProtectedDatabaseRestoreService>();
builder.Services.AddSingleton<IKeyedLockProvider, KeyedLockProvider>();
builder.Services.AddScoped<IPostCommitActionQueue, PostCommitActionQueue>();
/* builder.Services
    .AddHttpClient<IExternalNotificationService, ExternalNotificationService>(client =>
    {
        client.BaseAddress = new Uri("https://api.dummyemail.com/");
        client.Timeout = Timeout.InfiniteTimeSpan;
    })
    .AddResilienceHandler("external-notification", static pipeline =>
    {
        // 超时策略：单次请求最多执行 5 秒，超时后立即中断本次调用。
        pipeline.AddTimeout(TimeSpan.FromSeconds(5));

        // 重试策略：针对 5xx、网络异常、超时等瞬时故障最多重试 3 次，采用指数退避。
        pipeline.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = false,
            ShouldRetryAfterHeader = false,
            OnRetry = static args =>
            {
                Log.Warning(
                    "External notification API retry scheduled. Attempt={Attempt}; Delay={Delay}.",
                    args.AttemptNumber + 1,
                    args.RetryDelay);

                return default;
            }
        });

        // 断路器策略：30 秒窗口内达到 5 次瞬时故障时打开断路器 1 分钟，期间请求快速失败。
        pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            SamplingDuration = TimeSpan.FromSeconds(30),
            MinimumThroughput = 5,
            FailureRatio = 1.0,
            BreakDuration = TimeSpan.FromMinutes(1),
            OnOpened = static args =>
            {
                Log.Error(
                    "External notification API circuit opened for {BreakDuration}. Subsequent requests will fail fast.",
                    args.BreakDuration);

                return default;
            },
            OnHalfOpened = static args =>
            {
                Log.Warning("External notification API circuit is half-open. The next request will probe service recovery.");
                return default;
            },
            OnClosed = static args =>
            {
                Log.Information("External notification API circuit closed. Normal traffic has resumed.");
                return default;
            }
        });
    }); */
builder.Services.AddScoped<IEmailBackgroundQueue, EmailBackgroundQueue>();
builder.Services.AddHostedService<EmailDispatcherBackgroundService>();
builder.Services.AddSingleton(TypeAdapterConfig.GlobalSettings);
builder.Services.AddScoped<IMapper, ServiceMapper>();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    // Keep request logging first so command failures from validation and handlers reach the rolling file log.
    cfg.AddOpenBehavior(typeof(RequestLoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(AuditLoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(QuotaReadOnlyBehavior<,>));
    cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
    cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// --- Advanced Password Hashing: Argon2id ---
builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, Argon2idPasswordHasher<ApplicationUser>>();

// --- CSRF Protection (Hardened) ---
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN"; // Support for AJAX/Fetch
    options.Cookie.Name = ".ITPortal.XSRF.v2";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = cookieSecurePolicy;
});

// Configure SmtpSettings
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.Configure<SecuritySettings>(builder.Configuration.GetSection("SecuritySettings"));
builder.Services.Configure<PdpaComplianceOptions>(builder.Configuration.GetSection("PdpaCompliance"));
builder.Services.AddScoped<IOutlookService, OutlookService>();
builder.Services.AddSingleton<SecureOtpGenerator>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authentication.IClaimsTransformation, UserTitleClaimsTransformation>();
builder.Services.AddHostedService<OffboardingAutoSignService>();
builder.Services.AddHostedService<SlaCheckerService>();
builder.Services.AddHostedService<AssetLicenseExpiryService>();
builder.Services.AddHostedService<DatabaseBackupService>();

// --- Security Services ---
builder.Services.AddSingleton<ISignatureIntegrityService, SignatureIntegrityService>(); // HMAC signing (H-03)
builder.Services.AddSingleton<ILicenseEncryptionService, LicenseEncryptionService>();
builder.Services.AddScoped<ILicenseService, LicenseService>();
builder.Services.AddHostedService<DataRetentionService>();                               // PDPA retention (M-06)

// --- MVC ---
builder.Services.AddControllersWithViews(options =>
{
    // Enforce CSRF protection on all non-GET requests (POST, PUT, DELETE, etc.)
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
    options.Filters.Add<ValidationExceptionFilter>();
    options.Filters.Add<NotFoundExceptionFilter>();
    options.Filters.Add<QuotaReadOnlyFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "IT Form Parallel API",
            Version = context.DocumentName,
            Description = "OpenAPI surface for the hybrid MVC + Web API application."
        };

        var nonApiPaths = document.Paths.Keys
            .Where(path => !path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var path in nonApiPaths)
        {
            document.Paths.Remove(path);
        }

        document.Tags = new HashSet<OpenApiTag>(
            ApiOpenApiTagResolver.All.Select(tagName => new OpenApiTag { Name = tagName }));

        return Task.CompletedTask;
    });

    options.AddOperationTransformer((operation, context, cancellationToken) =>
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata ?? [];
        var httpMethod = context.Description.HttpMethod ?? string.Empty;
        var requiresAntiforgeryHeader =
            !HttpMethods.IsGet(httpMethod) &&
            !HttpMethods.IsHead(httpMethod) &&
            !HttpMethods.IsOptions(httpMethod) &&
            !metadata.OfType<AllowAnonymousAttribute>().Any() &&
            !metadata.OfType<IgnoreAntiforgeryTokenAttribute>().Any();

        if (requiresAntiforgeryHeader)
        {
            operation.Parameters ??= [];

            var hasXsrfHeader = operation.Parameters.Any(parameter =>
                parameter.In == ParameterLocation.Header &&
                string.Equals(parameter.Name, "X-XSRF-TOKEN", StringComparison.OrdinalIgnoreCase));

            if (!hasXsrfHeader)
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = "X-XSRF-TOKEN",
                    In = ParameterLocation.Header,
                    Required = true,
                    Description = "CSRF antiforgery token required for authenticated state-changing requests.",
                    Schema = new OpenApiSchema { Type = JsonSchemaType.String }
                });
            }
        }

        var tagName = ApiOpenApiTagResolver.Resolve(context.Description.RelativePath);
        if (!string.IsNullOrWhiteSpace(tagName))
        {
            operation.Tags = new HashSet<OpenApiTagReference>
            {
                new(tagName, context.Document, null)
            };
        }

        return Task.CompletedTask;
    });
});

// --- Health Checks ---
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(name: "database");

// --- Authorization Policies ---
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireIT", policy =>
        policy.RequireAssertion(ctx => PortalTitles.IsPrivilegedIt(ctx.User)));
    options.AddPolicy("RequireSuperAdmin", policy =>
        policy.RequireAssertion(ctx => PortalTitles.IsSuperAdmin(ctx.User)));
    options.AddPolicy("RequireHR", policy =>
        policy.RequireAssertion(ctx => ctx.User.HasClaim("UserTitle", "HR")));
    options.AddPolicy("RequireHROrIT", policy =>
        policy.RequireAssertion(ctx =>
            PortalTitles.IsPrivilegedIt(ctx.User) || ctx.User.HasClaim("UserTitle", PortalTitles.HR)));
    options.AddPolicy("RequireManager", policy =>
        policy.RequireAssertion(ctx => ctx.User.HasClaim("UserTitle", "Manager")));
});

// --- Rate Limiting (Availability) ---
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;
        opt.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("strict", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 10;
        opt.QueueLimit = 0;
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        if (context.HttpContext.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            await context.HttpContext.Response.WriteAsJsonAsync(new { error = "Too many requests. Please try again later." }, cancellationToken: token);
        }
        else
        {
            context.HttpContext.Response.ContentType = "text/html";
            await context.HttpContext.Response.WriteAsync(
                "<!DOCTYPE html><html><head><title>Too Many Requests</title>" +
                "<link href=\"http://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css\" rel=\"stylesheet\">" +
                "</head><body class=\"bg-light d-flex align-items-center justify-content-center\" style=\"height: 100vh;\">" +
                "<div class=\"text-center\"><h1 class=\"display-1 fw-bold text-danger\">429</h1><h2 class=\"mb-4\">Too Many Requests</h2>" +
                "<p class=\"lead\">You have exceeded the allowed number of requests. Please wait a moment and try again.</p>" +
                "<a href=\"/\" class=\"btn btn-primary mt-3\">Return to Home</a></div></body></html>", cancellationToken: token);
        }
    };
});

// --- Localization (Date Format) ---
var defaultCulture = new CultureInfo("en-GB");
defaultCulture.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
defaultCulture.DateTimeFormat.DateSeparator = "/";

var supportedCultures = new[] { defaultCulture };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture(defaultCulture);
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

// --- Reverse Proxy Forwarded Headers ---
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;

    var configuredProxies = builder.Configuration
        .GetSection("SecuritySettings:KnownProxies")
        .Get<string[]>() ?? [];

    if (configuredProxies.Length > 0)
    {
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();

        foreach (var configuredProxy in configuredProxies)
        {
            if (IPAddress.TryParse(configuredProxy, out var proxyAddress))
            {
                options.KnownProxies.Add(proxyAddress);
            }
        }
    }
});

// Bindings are managed via launchSettings.json or environment variables

var app = builder.Build();

app.Services
    .GetRequiredService<ProtectedDatabaseRestoreService>()
    .TryRestoreIfNeeded(GetSqliteDataSourcePath(resolvedDefaultConnectionString), dataDirectory);

// --- Middleware Pipeline ---
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}
app.Use(async (context, next) =>
{
    if (SensitiveFileStorage.IsLegacySensitiveUploadPath(context.Request.Path))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await next();
});
app.UseStaticFiles();
// Use Serilog to log every HTTP request
app.UseSerilogRequestLogging();

app.UseRateLimiter();

app.UseRequestLocalization();

app.UseAuthentication();
app.UseAuthorization();

// --- Password Change Enforcement Middleware ---
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true && context.User.HasClaim("MustChangePassword", "true"))
    {
        var path = context.Request.Path.Value ?? string.Empty;
        // Allow access to ForceChangePassword, Logout, Static Assets, and Health endpoints
        if (!path.StartsWith("/Account/ForceChangePassword", StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWith("/Account/Logout", StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWith("/images/", StringComparison.OrdinalIgnoreCase) &&
            !path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Redirect("/Account/ForceChangePassword");
            return;
        }
    }
    await next();
});

// --- Security Headers Middleware (Architect Level: CSP, X-Frame-Options, Masking) ---
app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseSession();

app.MapStaticAssets();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options
        .WithTitle("IT Form API Reference")
        .WithTheme(ScalarTheme.DeepSpace)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
        .WithOpenApiRoutePattern("/openapi/{documentName}.json"))
        .AllowAnonymous();
}
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Health check endpoint — accessible without auth so monitoring tools can ping it
// Visit /health to verify the app and database are up
app.MapHealthChecks("/health");

// --- Seed Database ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
        await DbInitializer.EnsureOtpPurposeColumnAsync(context);
        await DbInitializer.EnsureDefaultSlaSettingsAsync(context);
        await services.GetRequiredService<IQuotaControl>().EnsureDefaultsAsync();
        if (app.Environment.IsDevelopment())
        {
            DbInitializer.Seed(context);
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();
        await DbInitializer.SeedUsersAsync(userManager, context, configuration);

        var sensitiveUploadMigrationService = services.GetRequiredService<LegacySensitiveUploadMigrationService>();
        await sensitiveUploadMigrationService.MigrateAsync();

        var auditLogRepairService = services.GetRequiredService<AuditLogRepairService>();
        await auditLogRepairService.RepairAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating/seeding the database.");
    }
}

// --- Minimal API for Hardware Telemetry Sync ---
app.MapInventoryEndpoints();

app.Run();

static CookieSecurePolicy ResolveCookieSecurePolicy(IHostEnvironment environment, IConfiguration configuration)
{
    var configuredPolicy = configuration["SecuritySettings:CookieSecurePolicy"];
    if (!string.IsNullOrWhiteSpace(configuredPolicy))
    {
        if (string.Equals(configuredPolicy, "Never", StringComparison.OrdinalIgnoreCase))
        {
            return CookieSecurePolicy.None;
        }

        if (Enum.TryParse<CookieSecurePolicy>(configuredPolicy, ignoreCase: true, out var parsedPolicy))
        {
            return parsedPolicy;
        }

        throw new InvalidOperationException(
            "SecuritySettings:CookieSecurePolicy must be one of SameAsRequest, Always, or None.");
    }

    if (environment.IsDevelopment())
    {
        return CookieSecurePolicy.SameAsRequest;
    }

    var appDomain = configuration["AppDomain"];
    return Uri.TryCreate(appDomain, UriKind.Absolute, out var appUri) &&
        string.Equals(appUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
}

static string ResolveSqliteConnectionString(string? connectionString, string contentRootPath, string dataDirectory)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
    }

    const string dataSourcePrefix = "Data Source=";
    if (!connectionString.Contains(dataSourcePrefix, StringComparison.OrdinalIgnoreCase))
    {
        return connectionString;
    }

    var dataSourceIndex = connectionString.IndexOf(dataSourcePrefix, StringComparison.OrdinalIgnoreCase);
    var dataSourceStart = dataSourceIndex + dataSourcePrefix.Length;
    var separatorIndex = connectionString.IndexOf(';', dataSourceStart);
    var dataSourceEnd = separatorIndex >= 0 ? separatorIndex : connectionString.Length;
    var dbPath = connectionString[dataSourceStart..dataSourceEnd].Trim();

    const string dataDirectoryToken = "|DataDirectory|";
    if (dbPath.Contains(dataDirectoryToken, StringComparison.OrdinalIgnoreCase))
    {
        var relativeDbPath = dbPath[(dbPath.IndexOf(dataDirectoryToken, StringComparison.OrdinalIgnoreCase) + dataDirectoryToken.Length)..]
            .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        dbPath = Path.Combine(dataDirectory, relativeDbPath);
    }
    else if (!Path.IsPathRooted(dbPath))
    {
        dbPath = Path.Combine(contentRootPath, dbPath);
    }

    return connectionString[..dataSourceStart] + dbPath + connectionString[dataSourceEnd..];
}

static string GetSqliteDataSourcePath(string connectionString)
{
    var builder = new SqliteConnectionStringBuilder(connectionString);
    if (string.IsNullOrWhiteSpace(builder.DataSource))
    {
        throw new InvalidOperationException("SQLite connection string does not include a Data Source.");
    }

    return builder.DataSource;
}

/// <summary>
/// Defines the StartupUrlGuard class used by the IT Form application.
/// </summary>
internal static class StartupUrlGuard
{
    /// <summary>
    /// Runs the TryGetConflictingUrls operation.
    /// </summary>
    public static bool TryGetConflictingUrls(string? urls, out IReadOnlyCollection<string> conflictingUrls)
    {
        var conflicts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        conflictingUrls = conflicts;

        if (string.IsNullOrWhiteSpace(urls))
        {
            return false;
        }

        var listeningPorts = IPGlobalProperties
            .GetIPGlobalProperties()
            .GetActiveTcpListeners()
            .Select(endpoint => endpoint.Port)
            .ToHashSet();

        foreach (var rawUrl in urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri))
            {
                continue;
            }

            if (uri.Port > 0 && listeningPorts.Contains(uri.Port))
            {
                conflicts.Add(rawUrl);
            }
        }

        conflictingUrls = conflicts;
        return conflicts.Count > 0;
    }
}

/// <summary>
/// Defines the ApiOpenApiTagResolver class used by the IT Form application.
/// </summary>
internal static class ApiOpenApiTagResolver
{
    public static IReadOnlyCollection<string> All { get; } =
        ["Onboarding", "Offboarding", "Employees", "Inventory", "Audit Logs"];

    /// <summary>
    /// Runs the Resolve operation.
    /// </summary>
    public static string? Resolve(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var normalized = relativePath.TrimStart('/');
        return normalized switch
        {
            var path when path.StartsWith("api/onboarding-requests", StringComparison.OrdinalIgnoreCase) => "Onboarding",
            var path when path.StartsWith("api/offboarding-requests", StringComparison.OrdinalIgnoreCase) => "Offboarding",
            var path when path.StartsWith("api/employees", StringComparison.OrdinalIgnoreCase) => "Employees",
            var path when path.StartsWith("api/inventory", StringComparison.OrdinalIgnoreCase) => "Inventory",
            var path when path.StartsWith("api/audit-logs", StringComparison.OrdinalIgnoreCase) => "Audit Logs",
            _ => null
        };
    }
}


