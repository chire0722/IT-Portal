// Project comment: GetOffboardingEmployeeDetailsQuery.cs is part of the IT Form application source code.

using IT_Form.ViewModels;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.Create
{
    /// <summary>
    /// Carries data needed to execute the GetOffboardingEmployeeDetails query.
    /// </summary>
    public sealed record GetOffboardingEmployeeDetailsQuery(int Id) : IRequest<OffboardingEmployeeDetailsViewModel?>;
}
