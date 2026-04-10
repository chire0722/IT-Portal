using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.GetManageAccess
{
    public sealed record GetManageAccessQuery(int Id) : IRequest<ManageAccessViewModel?>;
}
