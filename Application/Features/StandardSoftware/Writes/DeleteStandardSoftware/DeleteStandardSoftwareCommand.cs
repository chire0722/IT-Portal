using MediatR;

namespace IT_Form.Application.Features.StandardSoftware.Writes.DeleteStandardSoftware;

public sealed record DeleteStandardSoftwareCommand(
    int Id,
    string? DeletedByUserId) : IRequest<Unit>;
