// Project comment: AllowedDomainAttribute.cs is part of the IT Form application source code.

using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace IT_Form.Validation
{
    /// <summary>
    /// Validates that an email address ends with one of the allowed corporate domains.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    /// <summary>
    /// Defines the AllowedDomainAttribute class used by the IT Form application.
    /// </summary>
    public class AllowedDomainAttribute : ValidationAttribute
    {
        private readonly string[] _allowedDomains;

        public AllowedDomainAttribute(params string[] allowedDomains)
        {
            _allowedDomains = allowedDomains;
            ErrorMessage = "Organization email required. Only @ytcprecast.com and @yengtong.com domains are allowed.";
        }

        /// <summary>
        /// Runs the IsValid operation.
        /// </summary>
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string email || string.IsNullOrWhiteSpace(email))
            {
                return new ValidationResult("Email address is required.");
            }

            // Basic email format check
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                return new ValidationResult("Please enter a valid email address.");
            }

            var domain = email.Split('@').LastOrDefault()?.ToLowerInvariant();

            if (domain == null || !_allowedDomains.Any(d => d.Equals(domain, StringComparison.OrdinalIgnoreCase)))
            {
                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }
    }
}
