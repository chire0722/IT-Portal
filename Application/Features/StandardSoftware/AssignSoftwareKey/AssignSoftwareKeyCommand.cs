// Project comment: AssignSoftwareKeyCommand.cs is part of the IT Form application source code.

using MediatR;

namespace IT_Form.Application.Features.StandardSoftware.AssignSoftwareKey
{
    /// <summary>
    /// Carries data needed to execute the AssignSoftwareKey command.
    /// </summary>
    public sealed record AssignSoftwareKeyCommand(
        int Id,
        int? OnboardingRequestId) : IRequest<AssignSoftwareKeyResult>;
}
