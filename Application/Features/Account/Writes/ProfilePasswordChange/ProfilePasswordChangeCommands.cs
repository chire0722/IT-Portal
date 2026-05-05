// Project comment: ProfilePasswordChangeCommands.cs contains authenticated profile password-change requests.

using MediatR;

namespace IT_Form.Application.Features.Account.Writes.ProfilePasswordChange;

public sealed record BeginProfilePasswordChangeCommand(
    string UserId,
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword,
    string ClientIp) : IRequest<BeginProfilePasswordChangeResult>;

public sealed record BeginProfilePasswordChangeResult(string RequestId, string Email);

public sealed record ConfirmProfilePasswordChangeCommand(
    string UserId,
    string RequestId,
    string Code) : IRequest<Unit>;

public sealed class ProfilePasswordChangeInputModel
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class ConfirmProfilePasswordChangeInputModel
{
    public string RequestId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
