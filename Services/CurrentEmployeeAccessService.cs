// Project comment: CurrentEmployeeAccessService.cs is part of the IT Form application source code.

using IT_Form.Data;
using IT_Form.Helpers;
using IT_Form.Models;
using IT_Form.Models.Enums;
using IT_Form.ViewModels;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Services
{
    /// <summary>
    /// Provides CurrentEmployeeAccess service behavior for the application.
    /// </summary>
    public sealed class CurrentEmployeeAccessService : ICurrentEmployeeAccessService
    {
        private readonly ApplicationDbContext _context;

        public CurrentEmployeeAccessService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets the data required by the GetEmployeeContextAsync operation.
        /// </summary>
        public async Task<EmployeeContext?> GetEmployeeContextAsync(int onboardingId, CancellationToken cancellationToken = default)
        {
            var request = await _context.OnboardingRequests
                .AsNoTracking()
                .AsSplitQuery()
                .Include(r => r.RequestEmailGroups)
                .Include(r => r.RequestSharedMailboxes)
                .Include(r => r.FolderPermissions)
                .FirstOrDefaultAsync(r => r.Id == onboardingId, cancellationToken);

            if (request == null || !IsCurrentEmployeeRecord(request))
            {
                return null;
            }

            var companyId = await ResolveCompanyIdAsync(request.Branch, cancellationToken);

            return new EmployeeContext
            {
                Request = request,
                CompanyId = companyId,
                CurrentDepartment = request.Department,
                CurrentBranch = request.Branch
            };
        }

        /// <summary>
        /// Gets the data required by the GetAccessCatalogAsync operation.
        /// </summary>
        public async Task<AccessCatalogDto> GetAccessCatalogAsync(
            int? companyId,
            IEnumerable<int>? existingEmailGroupIds,
            IEnumerable<int>? existingSharedMailboxIds,
            IEnumerable<FolderPermission>? existingFolderPermissions,
            CancellationToken cancellationToken = default)
        {
            var emailGroupsQuery = _context.EmailGroups
                .AsNoTracking()
                .Where(g => g.IsActive);

            emailGroupsQuery = companyId.HasValue
                ? emailGroupsQuery.Where(g => !g.EmailGroupCompanies.Any() || g.EmailGroupCompanies.Any(c => c.CompanyId == companyId.Value))
                : emailGroupsQuery.Where(g => !g.EmailGroupCompanies.Any());

            var sharedMailboxesQuery = _context.SharedMailboxes
                .AsNoTracking()
                .Where(m => m.IsActive);

            sharedMailboxesQuery = companyId.HasValue
                ? sharedMailboxesQuery.Where(m => !m.SharedMailboxCompanies.Any() || m.SharedMailboxCompanies.Any(c => c.CompanyId == companyId.Value))
                : sharedMailboxesQuery.Where(m => !m.SharedMailboxCompanies.Any());

            var serverFoldersQuery = _context.ServerFolders
                .AsNoTracking()
                .Where(f => f.IsActive);

            serverFoldersQuery = companyId.HasValue
                ? serverFoldersQuery.Where(f => !f.ServerFolderCompanies.Any() || f.ServerFolderCompanies.Any(c => c.CompanyId == companyId.Value))
                : serverFoldersQuery.Where(f => !f.ServerFolderCompanies.Any());

            var emailGroups = await emailGroupsQuery
                .OrderBy(g => g.GroupName)
                .ProjectToType<ManageAccessEmailGroupOptionViewModel>()
                .ToListAsync(cancellationToken);

            var sharedMailboxes = await sharedMailboxesQuery
                .OrderBy(m => m.MailboxName)
                .ProjectToType<ManageAccessSharedMailboxOptionViewModel>()
                .ToListAsync(cancellationToken);

            var serverFolders = await serverFoldersQuery
                .OrderBy(f => f.FolderName)
                .Select(f => f.FolderName)
                .ToListAsync(cancellationToken);

            var missingEmailGroupIds = (existingEmailGroupIds ?? Enumerable.Empty<int>())
                .Where(id => id > 0)
                .Except(emailGroups.Select(g => g.Id))
                .ToList();
            if (missingEmailGroupIds.Any())
            {
                emailGroups.AddRange(await _context.EmailGroups
                    .AsNoTracking()
                    .Where(g => missingEmailGroupIds.Contains(g.Id))
                    .ProjectToType<ManageAccessEmailGroupOptionViewModel>()
                    .ToListAsync(cancellationToken));

                emailGroups = emailGroups
                    .GroupBy(g => g.Id)
                    .Select(g => g.First())
                    .OrderBy(g => g.GroupName)
                    .ToList();
            }

            var missingSharedMailboxIds = (existingSharedMailboxIds ?? Enumerable.Empty<int>())
                .Where(id => id > 0)
                .Except(sharedMailboxes.Select(m => m.Id))
                .ToList();
            if (missingSharedMailboxIds.Any())
            {
                sharedMailboxes.AddRange(await _context.SharedMailboxes
                    .AsNoTracking()
                    .Where(m => missingSharedMailboxIds.Contains(m.Id))
                    .ProjectToType<ManageAccessSharedMailboxOptionViewModel>()
                    .ToListAsync(cancellationToken));

                sharedMailboxes = sharedMailboxes
                    .GroupBy(m => m.Id)
                    .Select(g => g.First())
                    .OrderBy(m => m.MailboxName)
                    .ToList();
            }

            var folderNames = serverFolders
                .Concat((existingFolderPermissions ?? Enumerable.Empty<FolderPermission>())
                    .Select(f => f.FolderName))
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new AccessCatalogDto
            {
                EmailGroups = emailGroups,
                SharedMailboxes = sharedMailboxes,
                ServerFolders = folderNames
                    .Select(name => name.Trim())
                    .OrderBy(name => name)
                    .ToList()
            };
        }

        /// <summary>
        /// Builds the value returned by the BuildFolderPermissions operation.
        /// </summary>
        public List<FolderPermissionViewModel> BuildFolderPermissions(
            IEnumerable<string> availableFolderNames,
            IEnumerable<FolderPermission>? existingPermissions)
        {
            var folderMap = new Dictionary<string, FolderPermissionViewModel>(StringComparer.OrdinalIgnoreCase);

            foreach (var folderName in availableFolderNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
            {
                var trimmedName = folderName.Trim();
                if (!folderMap.ContainsKey(trimmedName))
                {
                    folderMap[trimmedName] = new FolderPermissionViewModel
                    {
                        FolderName = trimmedName
                    };
                }
            }

            foreach (var permission in existingPermissions ?? Enumerable.Empty<FolderPermission>())
            {
                if (string.IsNullOrWhiteSpace(permission.FolderName))
                {
                    continue;
                }

                var trimmedName = permission.FolderName.Trim();
                if (!folderMap.TryGetValue(trimmedName, out var viewModel))
                {
                    viewModel = new FolderPermissionViewModel
                    {
                        FolderName = trimmedName
                    };
                    folderMap[trimmedName] = viewModel;
                }

                viewModel.CanRead = viewModel.CanRead || permission.CanRead;
                viewModel.CanModify = viewModel.CanModify || permission.CanModify;
            }

            return folderMap.Values
                .OrderBy(f => f.FolderName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Maps data for the MapLegacyFlags operation.
        /// </summary>
        public void MapLegacyFlags(
            IEnumerable<RequestEmailGroup> emailGroups,
            IEnumerable<RequestSharedMailbox> sharedMailboxes,
            OnboardingRequest target)
        {
            target.PrecastGroup = emailGroups.Any(group => AccessValueMatches(group.GroupName, group.GroupEmail, "precast"));

            target.MailboxAccount = sharedMailboxes.Any(mailbox => AccessValueMatches(mailbox.MailboxName, mailbox.MailboxAddress, "account", "m365"));
            target.MailboxLogistic = sharedMailboxes.Any(mailbox => AccessValueMatches(mailbox.MailboxName, mailbox.MailboxAddress, "logistic"));
            target.MailboxInfo = sharedMailboxes.Any(mailbox => AccessValueMatches(mailbox.MailboxName, mailbox.MailboxAddress, "info"));
            target.MailboxAdmin = sharedMailboxes.Any(mailbox => AccessValueMatches(mailbox.MailboxName, mailbox.MailboxAddress, "admin"));
            target.MailboxIntern = sharedMailboxes.Any(mailbox => AccessValueMatches(mailbox.MailboxName, mailbox.MailboxAddress, "intern"));
            target.MailboxIT = sharedMailboxes.Any(mailbox => AccessValueMatches(mailbox.MailboxName, mailbox.MailboxAddress, "it", "itdept"));
            target.MailboxSupervisor = sharedMailboxes.Any(mailbox => AccessValueMatches(mailbox.MailboxName, mailbox.MailboxAddress, "supervisor"));
            target.MailboxStore = sharedMailboxes.Any(mailbox => AccessValueMatches(mailbox.MailboxName, mailbox.MailboxAddress, "store"));
        }

        /// <summary>
        /// Runs the IsCurrentEmployeeRecord operation.
        /// </summary>
        private static bool IsCurrentEmployeeRecord(OnboardingRequest request)
            => request.Status == RequestStatus.Completed || request.Status == RequestStatus.Approved;

        /// <summary>
        /// Runs the ResolveCompanyIdAsync operation.
        /// </summary>
        private async Task<int?> ResolveCompanyIdAsync(string? branchName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(branchName))
            {
                return null;
            }

            var companies = await _context.Companies
                .Where(c => c.IsActive)
                .ToListAsync(cancellationToken);

            return CompanyImportMatchHelper.FindCompanyByReference(companies, branchName)?.Id;
        }

        /// <summary>
        /// Runs the AccessValueMatches operation.
        /// </summary>
        private static bool AccessValueMatches(string? primaryValue, string? secondaryValue, params string[] aliases)
        {
            var tokens = BuildAccessTokens(primaryValue)
                .Concat(BuildAccessTokens(secondaryValue))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return aliases
                .Select(NormalizeAccessToken)
                .Any(alias => tokens.Contains(alias));
        }

        /// <summary>
        /// Builds the value returned by the BuildAccessTokens operation.
        /// </summary>
        private static IEnumerable<string> BuildAccessTokens(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Enumerable.Empty<string>();
            }

            return System.Text.RegularExpressions.Regex.Matches(
                    value.ToLowerInvariant(),
                    "[a-z0-9]+")
                .Select(match => NormalizeAccessToken(match.Value))
                .Where(token => !string.IsNullOrWhiteSpace(token))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Runs the NormalizeAccessToken operation.
        /// </summary>
        private static string NormalizeAccessToken(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return new string(value
                .ToLowerInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray());
        }
    }
}
