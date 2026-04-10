using IT_Form.Models.Enums;
using IT_Form.Application.Common.Models;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.ExportOnboardingCsv
{
    public sealed record ExportOnboardingCsvQuery(
        string? SearchString,
        RequestStatus? Status) : IRequest<ProcessResult<BinaryFilePayload>>;
}
