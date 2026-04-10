using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.Create
{
    public sealed record GetOffboardingEmployeeDetailsQuery(int Id) : IRequest<OffboardingEmployeeDetailsViewModel?>;
}
