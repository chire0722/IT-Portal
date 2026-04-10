using IT_Form.Application.Common.Models;
using IT_Form.Models.Enums;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.ExportOffboardingCsv
{
    public sealed record ExportOffboardingCsvQuery(string? SearchString, RequestStatus? Status) : IRequest<ProcessResult<BinaryFilePayload>>;
}
