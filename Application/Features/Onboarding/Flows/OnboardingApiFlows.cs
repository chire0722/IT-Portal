using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Onboarding.GetOnboardingDetails;
using IT_Form.Application.Features.Onboarding.GetOnboardingIndex;
using IT_Form.Models.Enums;
using Mapster;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.Flows;

public sealed record GetOnboardingIndexApiQuery(string? SearchString, RequestStatus? Status, int Page = 1)
    : IRequest<OnboardingListApiResponse>;

public sealed record GetOnboardingDetailsApiQuery(int Id)
    : IRequest<ProcessResult<OnboardingDetailsApiResponse>>;

public sealed class GetOnboardingIndexApiQueryHandler
    : IRequestHandler<GetOnboardingIndexApiQuery, OnboardingListApiResponse>
{
    private readonly ISender _sender;

    public GetOnboardingIndexApiQueryHandler(ISender sender)
    {
        _sender = sender;
    }

    public async Task<OnboardingListApiResponse> Handle(GetOnboardingIndexApiQuery request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetOnboardingIndexQuery(request.SearchString, request.Status, request.Page), cancellationToken);
        return result.Adapt<OnboardingListApiResponse>();
    }
}

public sealed class GetOnboardingDetailsApiQueryHandler
    : IRequestHandler<GetOnboardingDetailsApiQuery, ProcessResult<OnboardingDetailsApiResponse>>
{
    private readonly ISender _sender;

    public GetOnboardingDetailsApiQueryHandler(ISender sender)
    {
        _sender = sender;
    }

    public async Task<ProcessResult<OnboardingDetailsApiResponse>> Handle(GetOnboardingDetailsApiQuery request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetOnboardingDetailsQuery(request.Id), cancellationToken);

        return result.Status switch
        {
            GetOnboardingDetailsStatus.NotFound => ProcessResult<OnboardingDetailsApiResponse>.NotFound(),
            GetOnboardingDetailsStatus.Forbidden => ProcessResult<OnboardingDetailsApiResponse>.Forbidden(),
            _ => ProcessResult<OnboardingDetailsApiResponse>.Success(result.ViewModel!.Adapt<OnboardingDetailsApiResponse>())
        };
    }
}
