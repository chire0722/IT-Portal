using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.Offboarding.GetOffboardingDetails;
using IT_Form.Application.Features.Offboarding.GetOffboardingIndex;
using IT_Form.Models.Enums;
using Mapster;
using MediatR;

namespace IT_Form.Application.Features.Offboarding.Flows;

public sealed record GetOffboardingIndexApiQuery(string? SearchString, RequestStatus? Status, int Page = 1)
    : IRequest<ProcessResult<OffboardingListApiResponse>>;

public sealed record GetOffboardingDetailsApiQuery(int Id)
    : IRequest<ProcessResult<OffboardingDetailsApiResponse>>;

public sealed class GetOffboardingIndexApiQueryHandler
    : IRequestHandler<GetOffboardingIndexApiQuery, ProcessResult<OffboardingListApiResponse>>
{
    private readonly ISender _sender;

    public GetOffboardingIndexApiQueryHandler(ISender sender)
    {
        _sender = sender;
    }

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

public sealed class GetOffboardingDetailsApiQueryHandler
    : IRequestHandler<GetOffboardingDetailsApiQuery, ProcessResult<OffboardingDetailsApiResponse>>
{
    private readonly ISender _sender;

    public GetOffboardingDetailsApiQueryHandler(ISender sender)
    {
        _sender = sender;
    }

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
