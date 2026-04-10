using IT_Form.Models.Enums;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.GetOnboardingIndex
{
    public sealed record GetOnboardingIndexQuery(
        string? SearchString,
        RequestStatus? Status,
        int Page = 1) : IRequest<GetOnboardingIndexResult>;
}
