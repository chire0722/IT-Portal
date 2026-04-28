// Project comment: OnboardingApiFlows.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Onboarding.GetOnboardingDetails;
using IT_Form.Application.Features.Onboarding.GetOnboardingIndex;
using IT_Form.Models.Enums;
using Mapster;
using MediatR;

namespace IT_Form.Application.Features.Onboarding.Flows;

/// <summary>
/// API query that lists onboarding requests with optional search, status filter, and paging.
/// </summary>
public sealed record GetOnboardingIndexApiQuery(string? SearchString, RequestStatus? Status, int Page = 1)
    : IRequest<OnboardingListApiResponse>;

/// <summary>
/// API query that fetches onboarding request details by id.
/// </summary>
public sealed record GetOnboardingDetailsApiQuery(int Id)
    : IRequest<ProcessResult<OnboardingDetailsApiResponse>>;

/// <summary>
/// Handles onboarding list API requests by reusing the normal onboarding index query.
/// </summary>
public sealed class GetOnboardingIndexApiQueryHandler
    : IRequestHandler<GetOnboardingIndexApiQuery, OnboardingListApiResponse>
{
    private readonly ISender _sender;

    /// <summary>
    /// Creates the API list handler with mediator access to the underlying page query.
    /// </summary>
    public GetOnboardingIndexApiQueryHandler(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Returns the onboarding index result mapped into the API response contract.
    /// </summary>
    public async Task<OnboardingListApiResponse> Handle(GetOnboardingIndexApiQuery request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetOnboardingIndexQuery(request.SearchString, request.Status, request.Page), cancellationToken);
        return result.Adapt<OnboardingListApiResponse>();
    }
}

/// <summary>
/// Handles onboarding details API requests by reusing the normal details query and mapping its status.
/// </summary>
public sealed class GetOnboardingDetailsApiQueryHandler
    : IRequestHandler<GetOnboardingDetailsApiQuery, ProcessResult<OnboardingDetailsApiResponse>>
{
    private readonly ISender _sender;

    /// <summary>
    /// Creates the API details handler with mediator access to the underlying details query.
    /// </summary>
    public GetOnboardingDetailsApiQueryHandler(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Returns details, not-found, or forbidden responses using the shared process-result shape.
    /// </summary>
    public async Task<ProcessResult<OnboardingDetailsApiResponse>> Handle(GetOnboardingDetailsApiQuery request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetOnboardingDetailsQuery(request.Id), cancellationToken);

        // Preserve authorization and existence outcomes instead of treating every result as a successful payload.
        return result.Status switch
        {
            GetOnboardingDetailsStatus.NotFound => ProcessResult<OnboardingDetailsApiResponse>.NotFound(),
            GetOnboardingDetailsStatus.Forbidden => ProcessResult<OnboardingDetailsApiResponse>.Forbidden(),
            _ => ProcessResult<OnboardingDetailsApiResponse>.Success(result.ViewModel!.Adapt<OnboardingDetailsApiResponse>())
        };
    }
}
