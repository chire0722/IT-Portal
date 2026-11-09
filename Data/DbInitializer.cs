// Project comment: DbInitializer.cs is part of the IT Form application source code.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace IT_Form.Data
{
    /// <summary>
    /// Defines the DbInitializer class used by the IT Form application.
    /// </summary>
    public static class DbInitializer
    {
        private static readonly string[] SeedAdminTitles = [PortalTitles.IT, PortalTitles.HR, PortalTitles.SuperAdmin];

        private static readonly SeedUserDefinition[] SeedUsers =
        [
            new("itdept", "it@itportal.com", "IT Administrator", "IT", "IT", ["it@ytcprecast.com"])
        ];

        /// <summary>
        /// Runs the readonly operation.
        /// </summary>
        private static readonly (string RequestType, int TargetDays, int EscalationDaysTier1, int EscalationDaysTier2)[] DefaultSlaSettings =
        {
            ("Onboarding", 3, 3, 7),
            ("Offboarding", 3, 3, 7)
        };

        /// <summary>
        /// Runs the EnsureOtpPurposeColumnAsync operation.
        /// </summary>
        public static async Task EnsureOtpPurposeColumnAsync(ApplicationDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            var shouldClose = connection.State != System.Data.ConnectionState.Open;
            if (shouldClose)
            {
                await connection.OpenAsync();
            }

            try
            {
                await using var checkCommand = connection.CreateCommand();
                checkCommand.CommandText = "PRAGMA table_info(\"OtpRecords\");";
                var hasPurposeColumn = false;
                await using (var reader = await checkCommand.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var columnName = reader["name"]?.ToString();
                        if (string.Equals(columnName, "Purpose", StringComparison.OrdinalIgnoreCase))
                        {
                            hasPurposeColumn = true;
                            break;
                        }
                    }
                }

                if (!hasPurposeColumn)
                {
                    await using var alterCommand = connection.CreateCommand();
                    alterCommand.CommandText = "ALTER TABLE \"OtpRecords\" ADD COLUMN \"Purpose\" TEXT NOT NULL DEFAULT 'PasswordReset';";
                    await alterCommand.ExecuteNonQueryAsync();
                }
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }
        }

        /// <summary>
        /// Runs the EnsureDefaultSlaSettingsAsync operation.
        /// </summary>
        public static async Task EnsureDefaultSlaSettingsAsync(ApplicationDbContext context)
        {
            var existingSettings = await context.SlaSettings.ToListAsync();
            bool hasChanges = false;

            var knownTypes = existingSettings
                .Select(s => NormalizeSlaRequestType(s.RequestType))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var defaultSetting in DefaultSlaSettings)
            {
                if (knownTypes.Contains(defaultSetting.RequestType))
                {
                    continue;
                }

                context.SlaSettings.Add(new SlaSetting
                {
                    RequestType = defaultSetting.RequestType,
                    TargetDays = defaultSetting.TargetDays,
                    EscalationDaysTier1 = defaultSetting.EscalationDaysTier1,
                    EscalationDaysTier2 = defaultSetting.EscalationDaysTier2,
                    UpdatedAt = DateTime.UtcNow
                });

                hasChanges = true;
            }

            if (hasChanges)
            {
                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Runs the NormalizeSlaRequestType operation.
        /// </summary>
        public static string NormalizeSlaRequestType(string? requestType)
        {
            if (string.IsNullOrWhiteSpace(requestType))
            {
                return string.Empty;
            }

            return requestType.Trim();
        }

        /// <summary>
        /// Runs the Seed operation.
        /// </summary>
        public static void Seed(ApplicationDbContext context)
        {
            // Only seed if there are no companies
            if (context.Companies.Any())
            {
                return;
            }

            // 1. Companies
            var companies = new[]
            {
                new Company { Name = "Global Tech Manufacturing (HQ)", Lot = "Lot 1A, Tech Park", IsActive = true },
                new Company { Name = "Global Tech Manufacturing (Plant 2)", Lot = "Lot 2B, Industrial Zone", IsActive = true },
                new Company { Name = "Aqua Logistics Hub", Lot = "Warehouse 5, Port Road", IsActive = true },
            };
            context.Companies.AddRange(companies);

            // 2. Departments
            var departments = new[]
            {
                new Department { Name = "Human Resources", IsActive = true },
                new Department { Name = "Information Technology", IsActive = true },
                new Department { Name = "Finance & Accounting", IsActive = true },
                new Department { Name = "Operations & Logistics", IsActive = true },
                new Department { Name = "Engineering", IsActive = true },
                new Department { Name = "Sales & Marketing", IsActive = true }
            };
            context.Departments.AddRange(departments);

            // 3. Shared Mailboxes
            var sharedMailboxes = new[]
            {
                new SharedMailbox { MailboxAddress = "info@globaltech.com", MailboxName = "General Information", IsActive = true },
                new SharedMailbox { MailboxAddress = "hr-support@globaltech.com", MailboxName = "HR Employee Support", IsActive = true },
                new SharedMailbox { MailboxAddress = "it-helpdesk@globaltech.com", MailboxName = "IT Support Desk", IsActive = true },
                new SharedMailbox { MailboxAddress = "sales-inquiries@globaltech.com", MailboxName = "Inbound Sales", IsActive = true }
            };
            context.SharedMailboxes.AddRange(sharedMailboxes);

            // 4. Standard Software
            var softwareList = new[]
            {
                new StandardSoftware
                {
                    SoftwareName = "Office 365",
                    IsActive = true,
                    Licenses = new List<SoftwareLicense>
                    {
                        new SoftwareLicense
                        {
                            SoftwareName = "Business Premium",
                            IsSubscription = true,
                            ContractLimit = 3,
                            Description = "Office tools + Advanced Security",
                            Keys = new List<SoftwareKey>
                            {
                                new SoftwareKey { KeyValue = "O365-BP-001", IsAssigned = true },
                                new SoftwareKey { KeyValue = "O365-BP-002", IsAssigned = false },
                                new SoftwareKey { KeyValue = "O365-BP-003", IsAssigned = false }
                            }
                        },
                        new SoftwareLicense
                        {
                            SoftwareName = "E3 Enterprise",
                            IsSubscription = true,
                            ContractLimit = 1,
                            Keys = new List<SoftwareKey> { new SoftwareKey { KeyValue = "O365-E3-100", IsAssigned = false } }
                        }
                    }
                },
                new StandardSoftware
                {
                    SoftwareName = "AutoCAD",
                    IsActive = true,
                    Licenses = new List<SoftwareLicense>
                    {
                        new SoftwareLicense
                        {
                            SoftwareName = "2024 Design Suite",
                            IsSubscription = false,
                            ContractLimit = 2,
                            Keys = new List<SoftwareKey>
                            {
                                new SoftwareKey { KeyValue = "ACAD-2024-X1Y2", IsAssigned = true },
                                new SoftwareKey { KeyValue = "ACAD-2024-A3B4", IsAssigned = false }
                            }
                        }
                    }
                },
                new StandardSoftware
                {
                    SoftwareName = "Adobe Creative Cloud",
                    IsActive = true,
                    Licenses = new List<SoftwareLicense>
                    {
                        new SoftwareLicense
                        {
                            SoftwareName = "All Apps",
                            IsSubscription = true,
                            ContractLimit = 1,
                            Keys = new List<SoftwareKey>
                            {
                                new SoftwareKey { KeyValue = "ADBH-CC-1111", IsAssigned = true }
                            }
                        }
                    }
                }
            };
            context.StandardSoftware.AddRange(softwareList);

            // 5. IT Assets
            var assets = new[]
            {
                new ITAsset { AssetTag = "AUTO-LT-001", SerialNumber = "PF21A8XZ", Model = "ThinkPad T14", CPU = "Core i7", RAM = "16", Storage = "512GB SSD", WindowsVersion = "Windows 11 Pro", Status = AssetStatus.Issued, AssetType = AssetType.Laptop },
                new ITAsset { AssetTag = "AUTO-LT-002", SerialNumber = "PF22B9Y1", Model = "Latitude 5430", CPU = "Core i5", RAM = "16", Storage = "256GB SSD", WindowsVersion = "Windows 10 Pro", Status = AssetStatus.Available, AssetType = AssetType.Laptop },
                new ITAsset { AssetTag = "AUTO-DT-101", SerialNumber = "MXL12345", Model = "OptiPlex 7090", CPU = "Core i7", RAM = "32", Storage = "1TB SSD", WindowsVersion = "Windows 11 Pro", Status = AssetStatus.Issued, AssetType = AssetType.DesktopPC },
                new ITAsset { AssetTag = "AUTO-MN-201", SerialNumber = "CN-0V4N7K", Model = "Dell U2722D", Status = AssetStatus.Available, AssetType = AssetType.Monitor },
                new ITAsset { AssetTag = "AUTO-SR-901", SerialNumber = "SVR-XY-44", Model = "PowerEdge R740", CPU = "Xeon Silver", RAM = "128", Storage = "10TB RAID", WindowsVersion = "Windows Server 2022", Status = AssetStatus.Available, AssetType = AssetType.Others }
            };
            context.ITAssets.AddRange(assets);
            context.SaveChanges();

            // 6. Employees and historical onboarding/offboarding requests
            var hqCompany = companies[0].Name;
            var hrDept = departments[0].Name;
            var itDept = departments[1].Name;
            var engDept = departments[4].Name;

            var alex = new OnboardingRequest
            {
                EmployeeID = "EMP-1001",
                FullName = "Alex Chen",
                JoinDate = DateTime.Today.AddDays(-120),
                Designation = "Senior Software Engineer",
                Branch = hqCompany,
                Department = engDept,
                HODEmail = "david.wong@globaltech.com",
                RequestDate = DateTime.Today.AddDays(-140),
                RequesterSignedAt = DateTime.Today.AddDays(-138),
                ITAdminSignedAt = DateTime.Today.AddDays(-135),
                Status = RequestStatus.Completed
            };

            // Assign Asset and Software to Alex
            var alexAsset = assets[0];
            alex.IssuedAssets = new List<IssuedAsset> {
                new IssuedAsset {
                    ITAssetId = alexAsset.Id,
                    AssetType = alexAsset.AssetType,
                    AssetTag = alexAsset.AssetTag,
                    BrandModel = alexAsset.Model
                }
            };

            var o365Lic = softwareList[0].Licenses.First();
            var o365Key = o365Lic.Keys.First(k => k.IsAssigned);
            var acadLic = softwareList[1].Licenses.First();
            var acadKey = acadLic.Keys.First(k => k.IsAssigned);

            alex.IssuedSoftware = new List<IssuedSoftware>
            {
                new IssuedSoftware { SoftwareLicenseId = o365Lic.Id, SoftwareName = softwareList[0].SoftwareName + " - " + o365Lic.SoftwareName },
                new IssuedSoftware { SoftwareLicenseId = acadLic.Id, SoftwareName = softwareList[1].SoftwareName + " - " + acadLic.SoftwareName }
            };

            var michael = new OnboardingRequest
            {
                EmployeeID = "EMP-1002",
                FullName = "Michael Rossi",
                JoinDate = DateTime.Today.AddDays(-45),
                Designation = "HR Generalist",
                Branch = hqCompany,
                Department = hrDept,
                HODEmail = "sarah.lee@globaltech.com",
                RequestDate = DateTime.Today.AddDays(-60),
                RequesterSignedAt = DateTime.Today.AddDays(-58),
                ITAdminSignedAt = DateTime.Today.AddDays(-55),
                Status = RequestStatus.Completed
            };

            var michaelAsset = assets[2];
            michael.IssuedAssets = new List<IssuedAsset> {
                new IssuedAsset {
                    ITAssetId = michaelAsset.Id,
                    AssetType = michaelAsset.AssetType,
                    AssetTag = michaelAsset.AssetTag,
                    BrandModel = michaelAsset.Model
                }
            };
            var adobeLic = softwareList[2].Licenses.First();
            var adobeKey = adobeLic.Keys.First(k => k.IsAssigned);
            michael.IssuedSoftware = new List<IssuedSoftware>
            {
                new IssuedSoftware { SoftwareLicenseId = adobeLic.Id, SoftwareName = softwareList[2].SoftwareName + " - " + adobeLic.SoftwareName }
            };

            var jessica = new OnboardingRequest
            {
                EmployeeID = "EMP-1003",
                FullName = "Jessica Lim",
                JoinDate = DateTime.Today.AddDays(5),
                Designation = "IT Support Specialist",
                Branch = companies[1].Name,
                Department = itDept,
                HODEmail = "it@ytcprecast.com",
                RequestDate = DateTime.Today.AddDays(-5),
                RequesterSignedAt = DateTime.Today.AddDays(-4),
                Status = RequestStatus.PendingITApproval
            };

            context.OnboardingRequests.AddRange(alex, michael, jessica);

            // Offboarding Example
            var offboarding = new OffboardingRequest
            {
                EmployeeID = "EMP-0999",
                FullName = "Robert Ford",
                Branch = companies[2].Name,
                Department = departments[3].Name,
                LastDayOfWork = DateTime.Today.AddDays(-10),
                HODEmail = "david.wong@globaltech.com",
                Status = RequestStatus.PendingHRApproval,
                RequestDate = DateTime.Today.AddDays(-38),
                HODSignedAt = DateTime.Today.AddDays(-36) // Missing HR/IT signatures
            };
            context.OffboardingRequests.Add(offboarding);

            context.SaveChanges();
        }

        /// <summary>
        /// Runs the SeedUsersAsync operation.
        /// </summary>
        public static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IConfiguration configuration)
        {
            // Never create or reset a privileged bootstrap account with a public default password.
            // Seed accounts are created only when an operator explicitly supplies a strong secret.
            var seedPassword = NormalizeSeedPassword(configuration["SeedAdmin:Password"]);
            if (seedPassword == null)
            {
                return;
            }

            var users = await context.Users
                .AsNoTracking()
                .Select(user => new
                {
                    user.Id,
                    user.UserName,
                    user.Email
                })
                .ToListAsync();

            if (users.Count == 0)
            {
                foreach (var seedUser in SeedUsers)
                {
                    await SeedUserAsync(
                        userManager,
                        context,
                        seedUser.UserName,
                        seedUser.Email,
                        seedUser.FullName,
                        seedUser.Department,
                        seedUser.Title,
                        seedPassword);
                }
            }
            else
            {
                var knownSeedEmails = SeedUsers
                    .SelectMany(seedUser => seedUser.AllEmails)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var knownSeedUserNames = SeedUsers
                    .Select(seedUser => seedUser.UserName)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var hasNonSeedAdminUsers = await context.Users
                    .AsNoTracking()
                    .Where(user => user.IsActive)
                    .Where(user =>
                        !knownSeedUserNames.Contains(user.UserName ?? string.Empty) &&
                        !knownSeedEmails.Contains(user.Email ?? string.Empty))
                    .Join(
                        context.UserTitles.AsNoTracking(),
                        user => user.Id,
                        title => title.UserId,
                        (user, title) => title.Title)
                    .AnyAsync(title => SeedAdminTitles.Contains(title));

                if (!hasNonSeedAdminUsers)
                {
                    foreach (var seedUser in SeedUsers)
                    {
                        await SeedUserAsync(
                            userManager,
                            context,
                            seedUser.UserName,
                            seedUser.Email,
                            seedUser.FullName,
                            seedUser.Department,
                            seedUser.Title,
                            seedPassword);
                    }

                    await SeedConfiguredSuperAdminAsync(userManager, context, configuration, seedPassword);
                    return;
                }
            }

            await SeedConfiguredSuperAdminAsync(userManager, context, configuration, seedPassword);

            if (users.Count > 0)
            {
                foreach (var seedUser in SeedUsers)
                {
                    foreach (var email in seedUser.AllEmails)
                    {
                        var existingSeedUser = await userManager.FindByEmailAsync(email);
                        if (existingSeedUser == null)
                        {
                            continue;
                        }

                        var isSuperAdminSeed = await context.UserTitles
                            .AsNoTracking()
                            .AnyAsync(title => title.UserId == existingSeedUser.Id && title.Title == PortalTitles.SuperAdmin);
                        if (isSuperAdminSeed)
                        {
                            continue;
                        }

                        var deleteResult = await userManager.DeleteAsync(existingSeedUser);
                        if (!deleteResult.Succeeded)
                        {
                            var errors = string.Join("; ", deleteResult.Errors.Select(error => error.Description));
                            throw new InvalidOperationException($"Failed to remove seed user '{email}': {errors}");
                        }
                    }
                }
            }
        }

        private static async Task SeedConfiguredSuperAdminAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IConfiguration configuration, string seedPassword)
        {
            var configuredEmail = NormalizeEmail(configuration["SeedSuperAdmin:Email"]);
            if (!string.IsNullOrWhiteSpace(configuredEmail))
            {
                await EnsureConfiguredSuperAdminAsync(userManager, context, configuration, configuredEmail, seedPassword);
                return;
            }

            var alreadyExists = await context.UserTitles
                .AnyAsync(title => title.Title == PortalTitles.SuperAdmin);
            if (alreadyExists)
            {
                return;
            }

            ApplicationUser? user = null;
            foreach (var email in SeedUsers
                .SelectMany(seedUser => seedUser.AllEmails)
                .Select(NormalizeEmail)
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                user = await FindUserByEmailAsync(context, email!);
                if (user != null)
                {
                    break;
                }
            }

            if (user == null)
            {
                return;
            }

            await PromoteUserToSuperAdminAsync(userManager, context, user, seedPassword);
        }

        private static async Task EnsureConfiguredSuperAdminAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IConfiguration configuration, string configuredEmail, string seedPassword)
        {
            var user = await FindUserByEmailAsync(context, configuredEmail);
            var userCreated = false;

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = configuredEmail,
                    Email = configuredEmail,
                    FullName = NormalizeDisplayName(configuration["SeedSuperAdmin:FullName"]) ?? "Super Admin",
                    Department = NormalizeDisplayName(configuration["SeedSuperAdmin:Department"]) ?? "IT",
                    JobTitle = NormalizeDisplayName(configuration["SeedSuperAdmin:JobTitle"]) ?? "Super Admin",
                    EmailConfirmed = true,
                    IsActive = true,
                    MustChangePassword = true
                };

                var createResult = await userManager.CreateAsync(user, seedPassword);
                if (!createResult.Succeeded)
                {
                    ThrowIdentityFailure($"Failed to create configured Super Admin '{configuredEmail}'", createResult);
                }

                userCreated = true;
            }

            // Existing accounts retain their active/confirmed state and password.
            // Startup may grant the explicitly configured role, but must not silently
            // reactivate an account or replace credentials.
            if (!userCreated)
            {
                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    user.Email = configuredEmail;
                }

                if (string.IsNullOrWhiteSpace(user.UserName))
                {
                    user.UserName = configuredEmail;
                }

                await EnsureExistingConfiguredSuperAdminCanBootstrapAsync(userManager, user, seedPassword);
            }

            await EnsureUserTitleAsync(context, user.Id, PortalTitles.SuperAdmin);
            await EnsureUserTitleAsync(context, user.Id, PortalTitles.IT);
            await context.SaveChangesAsync();
        }

        private static async Task PromoteUserToSuperAdminAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext context, ApplicationUser user, string seedPassword)
        {
            await EnsureUserTitleAsync(context, user.Id, PortalTitles.SuperAdmin);
            await EnsureUserTitleAsync(context, user.Id, PortalTitles.IT);
            await EnsureExistingConfiguredSuperAdminCanBootstrapAsync(userManager, user, seedPassword);
            await context.SaveChangesAsync();
        }

        private static async Task EnsureExistingConfiguredSuperAdminCanBootstrapAsync(UserManager<ApplicationUser> userManager, ApplicationUser user, string seedPassword)
        {
            if (!string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                return;
            }

            var passwordResult = await userManager.AddPasswordAsync(user, seedPassword);
            if (!passwordResult.Succeeded)
            {
                ThrowIdentityFailure("Failed to set Super Admin bootstrap password", passwordResult);
            }

            user.MustChangePassword = true;
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                ThrowIdentityFailure("Failed to update configured Super Admin", updateResult);
            }
        }

        private static async Task EnsureUserTitleAsync(ApplicationDbContext context, string userId, string title)
        {
            if (!await context.UserTitles.AnyAsync(item => item.UserId == userId && item.Title == title))
            {
                context.UserTitles.Add(new UserTitle { UserId = userId, Title = title });
            }
        }

        private static Task<ApplicationUser?> FindUserByEmailAsync(ApplicationDbContext context, string email)
        {
            var normalizedEmail = email.Trim().ToUpperInvariant();
            return context.Users
                .FirstOrDefaultAsync(user =>
                    user.NormalizedEmail == normalizedEmail ||
                    (user.Email != null && user.Email.ToUpper() == normalizedEmail));
        }

        /// <summary>
        /// Runs the SeedUserAsync operation.
        /// </summary>
        private static async Task SeedUserAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext context, string username, string email, string fullName, string department, string title, string password)
        {
            var existingUser = await userManager.FindByEmailAsync(email);
            if (existingUser == null)
            {
                var user = new ApplicationUser
                {
                    UserName = username,
                    Email = email,
                    FullName = fullName,
                    Department = department,
                    EmailConfirmed = true,
                    IsActive = true,
                    MustChangePassword = true
                };
                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    if (!await context.UserTitles.AnyAsync(ut => ut.UserId == user.Id && ut.Title == title))
                    {
                        context.UserTitles.Add(new UserTitle { UserId = user.Id, Title = title });
                        await context.SaveChangesAsync();
                    }
                }
            }
            else
            {
                // Ensure Title
                if (!await context.UserTitles.AnyAsync(ut => ut.UserId == existingUser.Id && ut.Title == title))
                {
                    context.UserTitles.Add(new UserTitle { UserId = existingUser.Id, Title = title });
                    await context.SaveChangesAsync();
                }

                // Keep the bootstrap password strictly first-use only. Do not reset an
                // existing seed user's password after they have completed first login.
                if (existingUser.UserName != username)
                {
                    existingUser.UserName = username;
                }

                if (string.IsNullOrWhiteSpace(existingUser.PasswordHash))
                {
                    existingUser.MustChangePassword = true;
                }

                await userManager.UpdateAsync(existingUser);
            }
        }

        /// <summary>
        /// Runs the NormalizeSeedPassword operation.
        /// </summary>
        private static string? NormalizeSeedPassword(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            if ((trimmed.StartsWith('<') && trimmed.EndsWith('>')) || trimmed.Length < 10)
            {
                return null;
            }

            return trimmed;
        }

        /// <summary>
        /// Runs the NormalizeEmail operation.
        /// </summary>
        private static string? NormalizeEmail(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }

        private static string? NormalizeDisplayName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }

        private static void ThrowIdentityFailure(string message, IdentityResult result)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"{message}: {errors}");
        }

        /// <summary>
        /// Defines the SeedUserDefinition record used by the IT Form application.
        /// </summary>
        private sealed record SeedUserDefinition(
            string UserName,
            string Email,
            string FullName,
            string Department,
            string Title,
            IReadOnlyCollection<string> LegacyEmails)
        {
            public IReadOnlyCollection<string> AllEmails => [Email, .. LegacyEmails];
        }
    }
}
