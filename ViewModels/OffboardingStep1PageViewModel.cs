// Project comment: OffboardingStep1PageViewModel.cs is part of the IT Form application source code.

using System.Reflection;
using IT_Form.Models;

namespace IT_Form.ViewModels
{
    /// <summary>
    /// View model for the first offboarding step, combining the request fields with page dropdown data.
    /// </summary>
    public sealed class OffboardingStep1PageViewModel : OffboardingRequest
    {
        /// <summary>
        /// Creates an empty page model for model binding.
        /// </summary>
        public OffboardingStep1PageViewModel()
        {
        }

        /// <summary>
        /// Copies editable offboarding request values into the page model.
        /// </summary>
        public OffboardingStep1PageViewModel(OffboardingRequest source)
        {
            // Reflection keeps this wrapper synchronized with OffboardingRequest without manual property mapping.
            foreach (var property in typeof(OffboardingRequest).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.CanRead && property.CanWrite)
                {
                    property.SetValue(this, property.GetValue(source));
                }
            }
        }

        /// <summary>
        /// Company names available for the step 1 company selector.
        /// </summary>
        public List<string> Companies { get; set; } = [];
    }
}
