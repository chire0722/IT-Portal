using IT_Form.Application.Common.Models;
using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.Step1
{
    public sealed record GetOffboardingStep1Query(int Id) : IRequest<ProcessResult<OffboardingStep1PageViewModel>>;
}
