// Project comment: DownloadOffboardingResignationQuery.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.DownloadOffboardingResignation
{
    /// <summary>
    /// Carries data needed to execute the DownloadOffboardingResignation query.
    /// </summary>
    public sealed record DownloadOffboardingResignationQuery(int Id) : IRequest<ProcessResult<BinaryFilePayload>>;
}
