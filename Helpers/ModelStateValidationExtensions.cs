// Project comment: ModelStateValidationExtensions.cs is part of the IT Form application source code.

using FluentValidation;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace IT_Form.Helpers
{
    /// <summary>
    /// Defines the ModelStateValidationExtensions class used by the IT Form application.
    /// </summary>
    public static class ModelStateValidationExtensions
    {
        /// <summary>
        /// Runs the AddFluentValidationErrors operation.
        /// </summary>
        public static void AddFluentValidationErrors(this ModelStateDictionary modelState, ValidationException exception)
        {
            foreach (var failure in exception.Errors)
            {
                var key = string.IsNullOrWhiteSpace(failure.PropertyName) ? string.Empty : failure.PropertyName;
                modelState.AddModelError(key, failure.ErrorMessage);
            }
        }
    }
}
