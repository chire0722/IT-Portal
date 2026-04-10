using MediatR;

namespace IT_Form.Application.Features.EmailContact.Writes.UpdateEmailContact;

public sealed record UpdateEmailContactCommand(
    int Id,
    string Name,
    string EmailAddress,
    string? Department) : IRequest<Unit>;
