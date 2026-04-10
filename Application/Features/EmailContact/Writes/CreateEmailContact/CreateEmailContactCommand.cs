using MediatR;

namespace IT_Form.Application.Features.EmailContact.Writes.CreateEmailContact;

public sealed record CreateEmailContactCommand(
    string Name,
    string EmailAddress,
    string? Department) : IRequest<int>;
