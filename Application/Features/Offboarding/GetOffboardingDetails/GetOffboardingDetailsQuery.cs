using IT_Form.Application.Common.Models;
using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.GetOffboardingDetails
{
    public sealed record GetOffboardingDetailsQuery(int Id) : IRequest<ProcessResult<OffboardingDetailsPageViewModel>>;
}
