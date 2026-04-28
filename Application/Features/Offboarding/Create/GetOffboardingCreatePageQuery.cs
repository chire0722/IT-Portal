// Project comment: GetOffboardingCreatePageQuery.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.Create
{
    /// <summary>
    /// Carries data needed to execute the GetOffboardingCreatePage query.
    /// </summary>
    public sealed record GetOffboardingCreatePageQuery(int? OnboardingRequestId = null)
        : IRequest<ProcessResult<OffboardingCreatePageViewModel>>;
}
