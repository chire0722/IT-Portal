using System.Reflection;
using IT_Form.Models;

namespace IT_Form.ViewModels
{
    public sealed class OffboardingStep1PageViewModel : OffboardingRequest
    {
        public OffboardingStep1PageViewModel()
        {
        }

        public OffboardingStep1PageViewModel(OffboardingRequest source)
        {
            foreach (var property in typeof(OffboardingRequest).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.CanRead && property.CanWrite)
                {
                    property.SetValue(this, property.GetValue(source));
                }
            }
        }

        public List<string> Companies { get; set; } = [];
    }
}
