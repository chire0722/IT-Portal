using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.ItSetup
{
    public sealed record StartOffboardingItSetupQuery(int Id) : IRequest<ProcessResult<object>>;
}
