// Project comment: GetManageAccessQuery.cs is part of the IT Form application source code.

using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.GetManageAccess
{
    /// <summary>
    /// Carries data needed to execute the GetManageAccess query.
    /// </summary>
    public sealed record GetManageAccessQuery(int Id) : IRequest<ManageAccessViewModel?>;
}
