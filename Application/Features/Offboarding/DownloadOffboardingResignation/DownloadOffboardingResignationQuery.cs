using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.DownloadOffboardingResignation
{
    public sealed record DownloadOffboardingResignationQuery(int Id) : IRequest<ProcessResult<BinaryFilePayload>>;
}
