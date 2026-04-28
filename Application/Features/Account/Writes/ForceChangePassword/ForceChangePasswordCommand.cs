// Project comment: ForceChangePasswordCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.Account.Writes.ForceChangePassword;

/// <summary>
/// Carries data needed to execute the ForceChangePassword command.
/// </summary>
public sealed record ForceChangePasswordCommand(
    string UserId,
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword) : IRequest<Unit>;
