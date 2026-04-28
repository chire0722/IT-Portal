// Project comment: ExportOffboardingCsvQuery.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Models.Enums;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.ExportOffboardingCsv
{
    /// <summary>
    /// Carries data needed to execute the ExportOffboardingCsv query.
    /// </summary>
    public sealed record ExportOffboardingCsvQuery(string? SearchString, RequestStatus? Status) : IRequest<ProcessResult<BinaryFilePayload>>;
}
