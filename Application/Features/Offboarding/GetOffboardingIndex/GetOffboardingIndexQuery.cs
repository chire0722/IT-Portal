// Project comment: GetOffboardingIndexQuery.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Models.Enums;
using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.GetOffboardingIndex
{
    /// <summary>
    /// Carries data needed to execute the GetOffboardingIndex query.
    /// </summary>
    public sealed record GetOffboardingIndexQuery(string? SearchString, RequestStatus? Status, int Page = 1)
        : IRequest<ProcessResult<OffboardingIndexPageViewModel>>;
}
