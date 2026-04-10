using FluentValidation;

namespace IT_Form.Application.Features.Company.SyncCompanyNames
{
    public sealed class SyncCompanyNamesCommandValidator : AbstractValidator<SyncCompanyNamesCommand>
    {
        public SyncCompanyNamesCommandValidator()
        {
        }
    }
}
