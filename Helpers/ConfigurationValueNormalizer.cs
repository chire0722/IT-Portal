namespace IT_Form.Helpers
{
    public static class ConfigurationValueNormalizer
    {
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
