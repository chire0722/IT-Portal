using MediatR;

namespace IT_Form.Application.Features.Account.Writes.ForceChangePassword;

public sealed record ForceChangePasswordCommand(
    string UserId,
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword) : IRequest<Unit>;
