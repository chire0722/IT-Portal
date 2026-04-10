using FluentValidation;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace IT_Form.Helpers
{
    public static class ModelStateValidationExtensions
    {
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
