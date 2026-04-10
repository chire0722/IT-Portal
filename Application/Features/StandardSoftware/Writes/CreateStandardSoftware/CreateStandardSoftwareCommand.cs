using MediatR;

namespace IT_Form.Application.Features.StandardSoftware.Writes.CreateStandardSoftware;

public sealed record CreateStandardSoftwareCommand(
    string SoftwareName,
    bool IsMandatory,
    bool IsFree,
    bool IsActive,
    bool HasMultipleVersions) : IRequest<int>;
