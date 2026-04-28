// Project comment: OffboardingApiFlows.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Offboarding.GetOffboardingDetails;
using IT_Form.Application.Features.Offboarding.GetOffboardingIndex;
using IT_Form.Models.Enums;
using Mapster;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.Flows;

/// <summary>
/// Carries data needed to execute the GetOffboardingIndexApi query.
/// </summary>
public sealed record GetOffboardingIndexApiQuery(string? SearchString, RequestStatus? Status, int Page = 1)
    : IRequest<ProcessResult<OffboardingListApiResponse>>;

/// <summary>
/// Carries data needed to execute the GetOffboardingDetailsApi query.
/// </summary>
public sealed record GetOffboardingDetailsApiQuery(int Id)
    : IRequest<ProcessResult<OffboardingDetailsApiResponse>>;

/// <summary>
/// Handles the GetOffboardingIndexApiQuery query workflow.
/// </summary>
public sealed class GetOffboardingIndexApiQueryHandler
    : IRequestHandler<GetOffboardingIndexApiQuery, ProcessResult<OffboardingListApiResponse>>
{
    private readonly ISender _sender;

    public GetOffboardingIndexApiQueryHandler(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<ProcessResult<OffboardingListApiResponse>> Handle(GetOffboardingIndexApiQuery request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetOffboardingIndexQuery(request.SearchString, request.Status, request.Page), cancellationToken);

        if (result.IsNotFound)
        {
            return ProcessResult<OffboardingListApiResponse>.NotFound();
        }

        if (result.IsForbidden)
        {
            return ProcessResult<OffboardingListApiResponse>.Forbidden();
        }

        return ProcessResult<OffboardingListApiResponse>.Success(result.Data!.Adapt<OffboardingListApiResponse>());
    }
}

/// <summary>
/// Handles the GetOffboardingDetailsApiQuery query workflow.
/// </summary>
public sealed class GetOffboardingDetailsApiQueryHandler
    : IRequestHandler<GetOffboardingDetailsApiQuery, ProcessResult<OffboardingDetailsApiResponse>>
{
    private readonly ISender _sender;

    public GetOffboardingDetailsApiQueryHandler(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<ProcessResult<OffboardingDetailsApiResponse>> Handle(GetOffboardingDetailsApiQuery request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetOffboardingDetailsQuery(request.Id), cancellationToken);

        if (result.IsNotFound)
        {
            return ProcessResult<OffboardingDetailsApiResponse>.NotFound();
        }

        if (result.IsForbidden)
        {
            return ProcessResult<OffboardingDetailsApiResponse>.Forbidden();
        }

        return ProcessResult<OffboardingDetailsApiResponse>.Success(result.Data!.Adapt<OffboardingDetailsApiResponse>());
    }
}
