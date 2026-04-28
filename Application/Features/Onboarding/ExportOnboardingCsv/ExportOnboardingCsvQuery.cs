// Project comment: ExportOnboardingCsvQuery.cs is part of the IT Form application source code.

using IT_Form.Models.Enums;
using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.ExportOnboardingCsv
{
    /// <summary>
    /// Carries data needed to execute the ExportOnboardingCsv query.
    /// </summary>
    public sealed record ExportOnboardingCsvQuery(
        string? SearchString,
        RequestStatus? Status) : IRequest<ProcessResult<BinaryFilePayload>>;
}
