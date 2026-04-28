// Project comment: GetOffboardingStep1Query.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.Step1
{
    /// <summary>
    /// Carries data needed to execute the GetOffboardingStep1 query.
    /// </summary>
    public sealed record GetOffboardingStep1Query(int Id) : IRequest<ProcessResult<OffboardingStep1PageViewModel>>;
}
