// Project comment: CompanyReferenceSyncService.cs is part of the IT Form application source code.

using System.Text.RegularExpressions;
using IT_Form.Data;
using IT_Form.Models;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Services
{
    /// <summary>
    /// Provides CompanyReferenceSync service behavior for the application.
    /// </summary>
    public class CompanyReferenceSyncService : ICompanyReferenceSyncService
    {
        private readonly ApplicationDbContext _context;
        private static readonly string[] LegalSuffixes =
        {
            "SDN BHD",
            "SDN. BHD.",
            "S/B",
            "BERHAD",
            "BHD",
            "PTE LTD",
            "PTE. LTD."
        };

        public CompanyReferenceSyncService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Applies changes for the ApplyUpdatedCompanySyncAsync operation.
        /// </summary>
        public async Task<int> ApplyUpdatedCompanySyncAsync(Company originalCompany, Company updatedCompany, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(originalCompany);
            ArgumentNullException.ThrowIfNull(updatedCompany);

            if (originalCompany.Id != 0 && updatedCompany.Id != 0 && originalCompany.Id != updatedCompany.Id)
            {
                throw new ArgumentException("The original and updated company must refer to the same record.", nameof(updatedCompany));
            }

            if (string.Equals(originalCompany.DisplayName, updatedCompany.DisplayName, StringComparison.Ordinal))
            {
                return 0;
            }

            var originalIdentity = CompanyReferenceIdentity.FromCompany(originalCompany);
            if (string.IsNullOrEmpty(originalIdentity.NormalizedName))
            {
                return 0;
            }

            return await ApplyCompanyReferenceUpdateAsync(
                currentValue => ShouldReplace(currentValue, originalIdentity, updatedCompany.DisplayName),
                updatedCompany.DisplayName,
                cancellationToken);
        }

        /// <summary>
        /// Runs the SyncAllActiveCompaniesAsync operation.
        /// </summary>
        public async Task<CompanyReferenceSyncResult> SyncAllActiveCompaniesAsync(CancellationToken cancellationToken = default)
        {
            var activeCompanies = await _context.Companies
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);

            var companyCandidates = activeCompanies
                .Select(company => new CompanyCandidate(company, CompanyReferenceIdentity.FromCompany(company)))
                .Where(candidate => !string.IsNullOrEmpty(candidate.Identity.NormalizedName))
                .ToList();

            var ambiguousReferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var resolutionCache = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            var updatedCount = await ApplyCompanyReferenceUpdateAsync(
                currentValue => ResolveReplacement(currentValue, companyCandidates, ambiguousReferences, resolutionCache),
                cancellationToken);

            return new CompanyReferenceSyncResult(updatedCount, ambiguousReferences.OrderBy(x => x).ToArray());
        }

        /// <summary>
        /// Applies changes for the ApplyCompanyReferenceUpdateAsync operation.
        /// </summary>
        private async Task<int> ApplyCompanyReferenceUpdateAsync(
            Func<string?, string?> replacementResolver,
            CancellationToken cancellationToken = default)
        {
            var updatedCount = 0;

            var onboardingRequests = await _context.OnboardingRequests
                .Where(r => r.Branch != null)
                .ToListAsync(cancellationToken);
            foreach (var request in onboardingRequests)
            {
                var replacement = replacementResolver(request.Branch);
                if (replacement != null)
                {
                    request.Branch = replacement;
                    updatedCount++;
                }
            }

            var offboardingRequests = await _context.OffboardingRequests
                .Where(r => r.Branch != null)
                .ToListAsync(cancellationToken);
            foreach (var request in offboardingRequests)
            {
                var replacement = replacementResolver(request.Branch);
                if (replacement != null)
                {
                    request.Branch = replacement;
                    updatedCount++;
                }
            }

            var userCompanies = await _context.UserCompanies.ToListAsync(cancellationToken);
            var companyNamesByUser = userCompanies
                .GroupBy(uc => uc.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => new HashSet<string>(
                        g.Select(uc => NormalizeForComparison(uc.CompanyName)),
                        StringComparer.Ordinal));

            foreach (var userCompany in userCompanies.ToList())
            {
                var replacement = replacementResolver(userCompany.CompanyName);
                if (replacement == null)
                {
                    continue;
                }

                var normalizedCurrentName = NormalizeForComparison(userCompany.CompanyName);
                var normalizedNewDisplayName = NormalizeForComparison(replacement);
                var assignedCompanyNames = companyNamesByUser[userCompany.UserId];
                assignedCompanyNames.Remove(normalizedCurrentName);

                // Avoid unique-index collisions when a user already has the renamed company assigned.
                if (assignedCompanyNames.Contains(normalizedNewDisplayName))
                {
                    _context.UserCompanies.Remove(userCompany);
                    updatedCount++;
                    continue;
                }

                userCompany.CompanyName = replacement;
                assignedCompanyNames.Add(normalizedNewDisplayName);
                updatedCount++;
            }

            return updatedCount;
        }

        /// <summary>
        /// Applies changes for the ApplyCompanyReferenceUpdateAsync operation.
        /// </summary>
        private static int ApplyCompanyReferenceUpdateAsync(
            Func<string?, bool> shouldReplace,
            string replacementValue,
            ref string? currentValue)
        {
            if (!shouldReplace(currentValue))
            {
                return 0;
            }

            currentValue = replacementValue;
            return 1;
        }

        /// <summary>
        /// Applies changes for the ApplyCompanyReferenceUpdateAsync operation.
        /// </summary>
        private async Task<int> ApplyCompanyReferenceUpdateAsync(
            Func<string?, bool> shouldReplace,
            string replacementValue,
            CancellationToken cancellationToken = default)
        {
            return await ApplyCompanyReferenceUpdateAsync(
                currentValue => shouldReplace(currentValue) ? replacementValue : null,
                cancellationToken);
        }

        /// <summary>
        /// Runs the ShouldReplace operation.
        /// </summary>
        private static bool ShouldReplace(string? currentValue, CompanyReferenceIdentity originalIdentity, string newDisplayName)
        {
            if (string.IsNullOrWhiteSpace(currentValue) ||
                string.Equals(currentValue, newDisplayName, StringComparison.Ordinal))
            {
                return false;
            }

            var currentIdentity = CompanyReferenceIdentity.FromReference(currentValue);
            return currentIdentity.Matches(originalIdentity);
        }

        /// <summary>
        /// Runs the ResolveReplacement operation.
        /// </summary>
        private static string? ResolveReplacement(
            string? currentValue,
            IReadOnlyCollection<CompanyCandidate> companyCandidates,
            ISet<string> ambiguousReferences,
            IDictionary<string, string?> resolutionCache)
        {
            if (string.IsNullOrWhiteSpace(currentValue))
            {
                return null;
            }

            if (resolutionCache.TryGetValue(currentValue, out var cachedReplacement))
            {
                return cachedReplacement;
            }

            var currentIdentity = CompanyReferenceIdentity.FromReference(currentValue);
            if (string.IsNullOrEmpty(currentIdentity.NormalizedName))
            {
                resolutionCache[currentValue] = null;
                return null;
            }

            var exactDisplayMatch = companyCandidates
                .Where(candidate => candidate.Identity.NormalizedDisplay == currentIdentity.NormalizedDisplay)
                .Select(candidate => candidate.Company.DisplayName)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (exactDisplayMatch.Count == 1)
            {
                var exactReplacement = exactDisplayMatch[0];
                resolutionCache[currentValue] = string.Equals(currentValue, exactReplacement, StringComparison.Ordinal)
                    ? null
                    : exactReplacement;
                return resolutionCache[currentValue];
            }

            var nameMatches = companyCandidates
                .Where(candidate => candidate.Identity.NormalizedName == currentIdentity.NormalizedName)
                .ToList();

            if (nameMatches.Count == 0)
            {
                resolutionCache[currentValue] = null;
                return null;
            }

            if (!string.IsNullOrEmpty(currentIdentity.NormalizedLot))
            {
                var lotMatches = nameMatches
                    .Where(candidate => string.Equals(candidate.Identity.NormalizedLot, currentIdentity.NormalizedLot, StringComparison.Ordinal))
                    .ToList();

                if (lotMatches.Count == 1)
                {
                    var lotReplacement = lotMatches[0].Company.DisplayName;
                    resolutionCache[currentValue] = string.Equals(currentValue, lotReplacement, StringComparison.Ordinal)
                        ? null
                        : lotReplacement;
                    return resolutionCache[currentValue];
                }

                if (lotMatches.Count > 1)
                {
                    ambiguousReferences.Add(currentValue);
                    resolutionCache[currentValue] = null;
                    return null;
                }
            }

            if (nameMatches.Count == 1)
            {
                var singleReplacement = nameMatches[0].Company.DisplayName;
                resolutionCache[currentValue] = string.Equals(currentValue, singleReplacement, StringComparison.Ordinal)
                    ? null
                    : singleReplacement;
                return resolutionCache[currentValue];
            }

            ambiguousReferences.Add(currentValue);
            resolutionCache[currentValue] = null;
            return null;
        }

        /// <summary>
        /// Runs the NormalizeForComparison operation.
        /// </summary>
        private static string NormalizeForComparison(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value.Trim();
            normalized = Regex.Replace(normalized, @"\s+", " ");
            normalized = Regex.Replace(normalized, @"\s*\(\s*", " (");
            normalized = Regex.Replace(normalized, @"\s*\)\s*", ")");

            return normalized.ToUpperInvariant();
        }

        /// <summary>
        /// Runs the NormalizeCompanyName operation.
        /// </summary>
        private static string NormalizeCompanyName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value.ToUpperInvariant();
            normalized = Regex.Replace(normalized, @"\([^)]*\)", " ");

            foreach (var suffix in LegalSuffixes)
            {
                normalized = normalized.Replace(suffix, " ", StringComparison.Ordinal);
            }

            normalized = Regex.Replace(normalized, @"[^A-Z0-9]+", " ");
            normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

            return normalized;
        }

        /// <summary>
        /// Runs the NormalizeLotKey operation.
        /// </summary>
        private static string NormalizeLotKey(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var uppercase = value.ToUpperInvariant();
            var lotMatch = Regex.Match(uppercase, @"LOT\s*([A-Z0-9\-\/]+)");
            if (lotMatch.Success)
            {
                return $"LOT{Regex.Replace(lotMatch.Groups[1].Value, @"[^A-Z0-9]+", string.Empty)}";
            }

            var firstSegment = uppercase.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(firstSegment))
            {
                return string.Empty;
            }

            var cleaned = Regex.Replace(firstSegment, @"[^A-Z0-9]+", string.Empty);
            return cleaned.Length <= 20 ? cleaned : string.Empty;
        }

        /// <summary>
        /// Defines the struct record used by the IT Form application.
        /// </summary>
        private readonly record struct CompanyCandidate(Company Company, CompanyReferenceIdentity Identity);

        /// <summary>
        /// Defines the struct record used by the IT Form application.
        /// </summary>
        private readonly record struct CompanyReferenceIdentity(
            string NormalizedDisplay,
            string NormalizedName,
            string NormalizedLot)
        {
            /// <summary>
            /// Runs the FromCompany operation.
            /// </summary>
            public static CompanyReferenceIdentity FromCompany(Company company)
            {
                ArgumentNullException.ThrowIfNull(company);

                return new CompanyReferenceIdentity(
                    NormalizeForComparison(company.DisplayName),
                    NormalizeCompanyName(company.Name),
                    NormalizeLotKey(company.Lot));
            }

            /// <summary>
            /// Runs the FromReference operation.
            /// </summary>
            public static CompanyReferenceIdentity FromReference(string? value)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return default;
                }

                var trimmed = value.Trim();
                var baseName = trimmed;
                var lotSource = trimmed;
                var firstParenIndex = trimmed.IndexOf('(');
                if (firstParenIndex >= 0)
                {
                    baseName = trimmed[..firstParenIndex];
                    var closingParenIndex = trimmed.IndexOf(')', firstParenIndex + 1);
                    lotSource = closingParenIndex > firstParenIndex
                        ? trimmed[(firstParenIndex + 1)..closingParenIndex]
                        : trimmed[(firstParenIndex + 1)..];
                }

                return new CompanyReferenceIdentity(
                    NormalizeForComparison(trimmed),
                    NormalizeCompanyName(baseName),
                    NormalizeLotKey(lotSource));
            }

            /// <summary>
            /// Runs the Matches operation.
            /// </summary>
            public bool Matches(CompanyReferenceIdentity other)
            {
                if (string.IsNullOrEmpty(NormalizedName) || string.IsNullOrEmpty(other.NormalizedName))
                {
                    return false;
                }

                if (!string.Equals(NormalizedName, other.NormalizedName, StringComparison.Ordinal))
                {
                    return false;
                }

                if (!string.IsNullOrEmpty(NormalizedLot) && !string.IsNullOrEmpty(other.NormalizedLot))
                {
                    return string.Equals(NormalizedLot, other.NormalizedLot, StringComparison.Ordinal);
                }

                return true;
            }
        }
    }
}
