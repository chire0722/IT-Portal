using IT_Form.Application.Common.Models;
using IT_Form.Models.Enums;
using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.GetOffboardingIndex
{
    public sealed record GetOffboardingIndexQuery(string? SearchString, RequestStatus? Status, int Page = 1)
        : IRequest<ProcessResult<OffboardingIndexPageViewModel>>;
}
