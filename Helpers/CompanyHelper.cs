using System;

namespace IT_Form.Helpers
{
    public static class CompanyHelper
    {
        /// <summary>
        /// Normalizes a company name by removing the lot address in parentheses.
        /// Example: "YTC Precast (Lot 123)" -> "YTC Precast"
        /// </summary>
        public static string Normalize(string? companyName)
        {
            if (string.IsNullOrWhiteSpace(companyName)) return string.Empty;

            int parenIndex = companyName.IndexOf(" (");
            if (parenIndex > 0)
            {
                return companyName.Substring(0, parenIndex).Trim();
            }
            return companyName.Trim();
        }

        /// <summary>
        /// Compares two company names.
        /// Rule: "Name" matches "Name (Lot 1)". But "Name (Lot 1)" does NOT match "Name (Lot 2)".
        /// </summary>
        public static bool IsMatch(string? name1, string? name2)
        {
            if (name1 == null || name2 == null) return false;
            if (string.Equals(name1.Trim(), name2.Trim(), StringComparison.OrdinalIgnoreCase)) return true;

            bool hasLot1 = name1.Contains(" (");
            bool hasLot2 = name2.Contains(" (");

            // If both sides have lot information and they didn't match exactly above, it's a mismatch
            if (hasLot1 && hasLot2) return false;

            // Either one or both don't have lot information, so we compare base names
            string norm1 = Normalize(name1);
            string norm2 = Normalize(name2);

            return string.Equals(norm1, norm2, StringComparison.OrdinalIgnoreCase);
        }
    }
}
