// Project comment: StartOffboardingItSetupQuery.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.ItSetup
{
    /// <summary>
    /// Carries data needed to execute the StartOffboardingItSetup query.
    /// </summary>
    public sealed record StartOffboardingItSetupQuery(int Id) : IRequest<ProcessResult<object>>;
}
