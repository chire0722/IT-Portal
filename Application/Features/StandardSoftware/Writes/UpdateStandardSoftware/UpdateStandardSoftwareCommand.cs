using MediatR;

namespace IT_Form.Application.Features.StandardSoftware.Writes.UpdateStandardSoftware;

public sealed record UpdateStandardSoftwareCommand(
    int Id,
    string SoftwareName,
    bool IsMandatory,
    bool IsFree,
    bool IsActive,
    bool HasMultipleVersions) : IRequest<Unit>;
