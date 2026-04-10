using MediatR;

namespace IT_Form.Application.Features.StandardSoftware.AssignSoftwareKey
{
    public sealed record AssignSoftwareKeyCommand(
        int Id,
        int? OnboardingRequestId) : IRequest<AssignSoftwareKeyResult>;
}
