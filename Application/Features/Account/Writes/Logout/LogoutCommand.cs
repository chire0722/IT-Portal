using MediatR;

namespace IT_Form.Application.Features.Account.Writes.Logout;

public sealed record LogoutCommand() : IRequest<Unit>;
