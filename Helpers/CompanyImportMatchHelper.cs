using System.Text.RegularExpressions;
using IT_Form.Models;

namespace IT_Form.Helpers
{
    public static class CompanyImportMatchHelper
    {
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

        public static Company? FindCompanyByReference(IEnumerable<Company> companies, string? reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                return null;
            }

            var referenceIdentity = CompanyReferenceIdentity.FromReference(reference);
            if (string.IsNullOrEmpty(referenceIdentity.NormalizedName))
            {
                return null;
            }

            var companyList = companies.ToList();

            var exactDisplayMatches = companyList
                .Where(company => string.Equals(
                    NormalizeForComparison(company.DisplayName),
                    referenceIdentity.NormalizedDisplay,
                    StringComparison.Ordinal))
                .ToList();

            if (exactDisplayMatches.Count == 1)
            {
                return exactDisplayMatches[0];
            }

            var nameMatches = companyList
                .Where(company => string.Equals(
                    NormalizeCompanyName(company.Name),
                    referenceIdentity.NormalizedName,
                    StringComparison.Ordinal))
                .ToList();

            if (!string.IsNullOrEmpty(referenceIdentity.NormalizedLot))
            {
                var lotMatches = nameMatches
                    .Where(company => string.Equals(
                        NormalizeLotKey(company.Lot),
                        referenceIdentity.NormalizedLot,
                        StringComparison.Ordinal))
                    .ToList();

                if (lotMatches.Count == 1)
                {
                    return lotMatches[0];
                }
            }

            if (nameMatches.Count == 1)
            {
                return nameMatches[0];
            }

            var referenceTokens = TokenizeReference(reference);
            if (referenceTokens.Count == 0)
            {
                return null;
            }

            var partialMatches = companyList
                .Where(company => CompanyMatchesTokens(company, referenceTokens))
                .ToList();

            return partialMatches.Count == 1 ? partialMatches[0] : null;
        }

        public static List<string> SplitCompanyReferences(string? input)
        {
            var results = new List<string>();
            if (string.IsNullOrWhiteSpace(input))
            {
                return results;
            }

            var current = new System.Text.StringBuilder();
            var parenthesisDepth = 0;

            foreach (var character in input)
            {
                if (character == '(')
                {
                    parenthesisDepth++;
                }
                else if (character == ')' && parenthesisDepth > 0)
                {
                    parenthesisDepth--;
                }

                if ((character == ',' || character == ';') && parenthesisDepth == 0)
                {
                    AddSegment(results, current);
                    continue;
                }

                current.Append(character);
            }

            AddSegment(results, current);
            return results;
        }

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

        private static void AddSegment(List<string> results, System.Text.StringBuilder current)
        {
            var value = current.ToString().Trim();
            if (!string.IsNullOrEmpty(value))
            {
                results.Add(value);
            }

            current.Clear();
        }

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
            if (cleaned.Length == 0 || cleaned.Length > 20)
            {
                return string.Empty;
            }

            if (cleaned.StartsWith("LOT", StringComparison.Ordinal) || cleaned.All(char.IsDigit))
            {
                return cleaned;
            }

            return string.Empty;
        }

        private static bool CompanyMatchesTokens(Company company, IReadOnlyCollection<string> referenceTokens)
        {
            var companyTokens = BuildCompanyTokens(company);
            return referenceTokens.All(companyTokens.Contains);
        }

        private static HashSet<string> BuildCompanyTokens(Company company)
        {
            var tokens = new HashSet<string>(StringComparer.Ordinal);

            AddNormalizedTokens(tokens, company.DisplayName, keepLegalSuffixes: false);
            AddNormalizedTokens(tokens, company.Name, keepLegalSuffixes: false);
            AddNormalizedTokens(tokens, company.Lot, keepLegalSuffixes: true);

            var normalizedLot = NormalizeLotKey(company.Lot);
            if (!string.IsNullOrEmpty(normalizedLot))
            {
                tokens.Add(normalizedLot);

                var strippedLot = StripLotPrefix(normalizedLot);
                if (!string.IsNullOrEmpty(strippedLot))
                {
                    tokens.Add(strippedLot);
                }
            }

            return tokens;
        }

        private static HashSet<string> TokenizeReference(string reference)
        {
            var tokens = new HashSet<string>(StringComparer.Ordinal);

            AddNormalizedTokens(tokens, reference, keepLegalSuffixes: false);

            var normalizedLot = NormalizeLotKey(reference);
            if (!string.IsNullOrEmpty(normalizedLot))
            {
                tokens.Add(normalizedLot);

                var strippedLot = StripLotPrefix(normalizedLot);
                if (!string.IsNullOrEmpty(strippedLot))
                {
                    tokens.Add(strippedLot);
                }
            }

            return tokens;
        }

        private static void AddNormalizedTokens(HashSet<string> tokens, string? value, bool keepLegalSuffixes)
        {
            foreach (var token in NormalizeToSearchTokens(value, keepLegalSuffixes))
            {
                tokens.Add(token);
            }
        }

        private static IEnumerable<string> NormalizeToSearchTokens(string? value, bool keepLegalSuffixes)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Array.Empty<string>();
            }

            var normalized = value.ToUpperInvariant();
            normalized = Regex.Replace(normalized, @"\([^)]*\)", " ");

            if (!keepLegalSuffixes)
            {
                foreach (var suffix in LegalSuffixes)
                {
                    normalized = normalized.Replace(suffix, " ", StringComparison.Ordinal);
                }
            }

            normalized = Regex.Replace(normalized, @"[^A-Z0-9]+", " ");

            return normalized
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(IsMeaningfulSearchToken);
        }

        private static bool IsMeaningfulSearchToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            return token.All(char.IsDigit)
                ? token.Length >= 2
                : token.Length >= 3;
        }

        private static string StripLotPrefix(string token)
        {
            if (!token.StartsWith("LOT", StringComparison.Ordinal) || token.Length <= 3)
            {
                return string.Empty;
            }

            return token[3..];
        }

        private readonly record struct CompanyReferenceIdentity(
            string NormalizedDisplay,
            string NormalizedName,
            string NormalizedLot)
        {
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
        }
    }
}
