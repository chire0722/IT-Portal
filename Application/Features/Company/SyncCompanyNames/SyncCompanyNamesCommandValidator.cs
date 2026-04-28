// Project comment: SyncCompanyNamesCommandValidator.cs is part of the IT Form application source code.

using FluentValidation;

namespace IT_Form.Application.Features.Company.SyncCompanyNames
{
    /// <summary>
    /// Validates input for the SyncCompanyNamesCommand request.
    /// </summary>
    public sealed class SyncCompanyNamesCommandValidator : AbstractValidator<SyncCompanyNamesCommand>
    {
        public SyncCompanyNamesCommandValidator()
        {
        }
    }
}
