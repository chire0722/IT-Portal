// Project comment: ConfigurationValueNormalizer.cs is part of the IT Form application source code.

namespace IT_Form.Helpers
{
    /// <summary>
    /// Defines the ConfigurationValueNormalizer class used by the IT Form application.
    /// </summary>
    public static class ConfigurationValueNormalizer
    {
        /// <summary>
        /// Runs the NormalizeSecret operation.
        /// </summary>
        public static string NormalizeSecret(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var trimmed = value.Trim();
            if (trimmed.StartsWith('<') && trimmed.EndsWith('>'))
            {
                return string.Empty;
            }

            return trimmed;
        }

        /// <summary>
        /// Runs the NormalizeBaseUrl operation.
        /// </summary>
        public static string? NormalizeBaseUrl(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value.Trim().TrimEnd('/', ';');
            if (!Uri.TryCreate(normalized, UriKind.Absolute, out var parsed))
            {
                return null;
            }

            if (parsed.Host is "0.0.0.0" or "::")
            {
                return null;
            }

            return $"{parsed.Scheme}://{parsed.Authority}";
        }
    }
}
