using IT_Form.Application.Common.Models;
using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.Create
{
    public sealed record GetOffboardingCreatePageQuery(int? OnboardingRequestId = null)
        : IRequest<ProcessResult<OffboardingCreatePageViewModel>>;
}
