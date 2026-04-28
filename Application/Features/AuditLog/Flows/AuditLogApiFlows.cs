// Project comment: AuditLogApiFlows.cs is part of the IT Form application source code.

using IT_Form.Application.Common.Models;
using IT_Form.Application.Features.AuditLogs.Shared;
using IT_Form.Data;
using IT_Form.Models;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IT_Form.Application.Features.AuditLogs.Flows;

/// <summary>
/// Carries data needed to execute the GetAuditLogListApi query.
/// </summary>
public sealed record GetAuditLogListApiQuery(
    string? Entity,
    string? AuditAction,
    string? Search,
    DateTime? StartDate,
    DateTime? EndDate,
    bool? IsSuccess,
    int Page = 1) : IRequest<AuditLogListApiResponse>;

/// <summary>
/// Carries data needed to execute the GetAuditLogDetailsApi query.
/// </summary>
public sealed record GetAuditLogDetailsApiQuery(int Id)
    : IRequest<ProcessResult<AuditLogDetailsApiResponse>>;

/// <summary>
/// Carries data needed to execute the GetAuditLogChainApi query.
/// </summary>
public sealed record GetAuditLogChainApiQuery(int Id, bool FailuresOnly = false)
    : IRequest<ProcessResult<AuditLogChainApiResponse>>;

/// <summary>
/// Carries data needed to execute the GetAuditLogSummaryApi query.
/// </summary>
public sealed record GetAuditLogSummaryApiQuery(
    string? Entity,
    string? AuditAction,
    string? Search,
    DateTime? StartDate,
    DateTime? EndDate,
    bool? IsSuccess) : IRequest<AuditLogSummaryApiResponse>;

/// <summary>
/// Handles the GetAuditLogListApiQuery query workflow.
/// </summary>
public sealed class GetAuditLogListApiQueryHandler
    : IRequestHandler<GetAuditLogListApiQuery, AuditLogListApiResponse>
{
    private const int PageSize = 25;
    private readonly ApplicationDbContext _context;

    public GetAuditLogListApiQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<AuditLogListApiResponse> Handle(GetAuditLogListApiQuery request, CancellationToken cancellationToken)
    {
        var filter = request.ToFilterParameters();
        var query = AuditLogReadSupport.ApplyFilters(_context.AuditLogs.AsNoTracking(), filter);
        var page = request.Page < 1 ? 1 : request.Page;
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Timestamp)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ProjectToType<AuditLogListItemApiResponse>()
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

        return new AuditLogListApiResponse(
            new ApiPageMetadata(
                page,
                totalPages,
                totalCount,
                page > 1,
                page < totalPages),
            request.Entity,
            request.AuditAction,
            request.Search,
            request.StartDate,
            request.EndDate,
            request.IsSuccess,
            items);
    }
}

/// <summary>
/// Handles the GetAuditLogDetailsApiQuery query workflow.
/// </summary>
public sealed class GetAuditLogDetailsApiQueryHandler
    : IRequestHandler<GetAuditLogDetailsApiQuery, ProcessResult<AuditLogDetailsApiResponse>>
{
    private readonly ApplicationDbContext _context;

    public GetAuditLogDetailsApiQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<ProcessResult<AuditLogDetailsApiResponse>> Handle(GetAuditLogDetailsApiQuery request, CancellationToken cancellationToken)
    {
        var log = await _context.AuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        return log == null
            ? ProcessResult<AuditLogDetailsApiResponse>.NotFound()
            : ProcessResult<AuditLogDetailsApiResponse>.Success(log.Adapt<AuditLogDetailsApiResponse>());
    }
}

/// <summary>
/// Handles the GetAuditLogChainApiQuery query workflow.
/// </summary>
public sealed class GetAuditLogChainApiQueryHandler
    : IRequestHandler<GetAuditLogChainApiQuery, ProcessResult<AuditLogChainApiResponse>>
{
    private readonly ApplicationDbContext _context;

    public GetAuditLogChainApiQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<ProcessResult<AuditLogChainApiResponse>> Handle(GetAuditLogChainApiQuery request, CancellationToken cancellationToken)
    {
        var anchor = await _context.AuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (anchor == null)
        {
            return ProcessResult<AuditLogChainApiResponse>.NotFound();
        }

        var fullChainQuery = anchor.CorrelationId.HasValue
            ? _context.AuditLogs
                .AsNoTracking()
                .Where(a => a.CorrelationId == anchor.CorrelationId)
            : _context.AuditLogs
                .AsNoTracking()
                .Where(a => a.Id == anchor.Id);

        var fullCount = await fullChainQuery.CountAsync(cancellationToken);
        var failureCount = await fullChainQuery.CountAsync(a => !a.IsSuccess, cancellationToken);

        var itemsQuery = request.FailuresOnly
            ? fullChainQuery.Where(a => !a.IsSuccess || a.Id == anchor.Id)
            : fullChainQuery;

        var items = await itemsQuery
            .OrderBy(a => a.CreatedAt)
            .ThenBy(a => a.Timestamp)
            .ThenBy(a => a.Id)
            .ToListAsync(cancellationToken);

        return ProcessResult<AuditLogChainApiResponse>.Success(new AuditLogChainApiResponse(
            anchor.Id,
            anchor.CorrelationId,
            anchor.CorrelationId.HasValue,
            request.FailuresOnly,
            fullCount,
            failureCount,
            items.Adapt<List<AuditLogChainItemApiResponse>>()));
    }
}

/// <summary>
/// Handles the GetAuditLogSummaryApiQuery query workflow.
/// </summary>
public sealed class GetAuditLogSummaryApiQueryHandler
    : IRequestHandler<GetAuditLogSummaryApiQuery, AuditLogSummaryApiResponse>
{
    private readonly ApplicationDbContext _context;

    public GetAuditLogSummaryApiQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Executes the request and returns the workflow result.
    /// </summary>
    public async Task<AuditLogSummaryApiResponse> Handle(GetAuditLogSummaryApiQuery request, CancellationToken cancellationToken)
    {
        var filter = request.ToFilterParameters();
        var query = AuditLogReadSupport.ApplyFilters(_context.AuditLogs.AsNoTracking(), filter);
        var failureQuery = query.Where(a => !a.IsSuccess);

        var totalCount = await query.CountAsync(cancellationToken);
        var successCount = await query.CountAsync(a => a.IsSuccess, cancellationToken);
        var failureCount = await failureQuery.CountAsync(cancellationToken);
        var commandCount = await query.CountAsync(a => a.EntityName == "Command", cancellationToken);
        var failedCommandCount = await query.CountAsync(a => a.EntityName == "Command" && !a.IsSuccess, cancellationToken);

        var topFailedActions = (await failureQuery
            .Where(a => !string.IsNullOrWhiteSpace(a.Action))
            .GroupBy(a => a.Action)
            .Select(group => new
            {
                Label = group.Key!,
                Count = group.Count()
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Label)
            .Take(5)
            .ToListAsync(cancellationToken))
            .Select(x => new AuditLogBreakdownApiResponse(x.Label, x.Count))
            .ToList();

        var topFailedActors = (await failureQuery
            .Select(a => new
            {
                Actor = !string.IsNullOrWhiteSpace(a.PerformedBy) ? a.PerformedBy : a.UserId
            })
            .Where(a => !string.IsNullOrWhiteSpace(a.Actor))
            .GroupBy(a => a.Actor)
            .Select(group => new
            {
                Label = group.Key!,
                Count = group.Count()
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Label)
            .Take(5)
            .ToListAsync(cancellationToken))
            .Select(x => new AuditLogBreakdownApiResponse(x.Label, x.Count))
            .ToList();

        var failureTrend = (await failureQuery
            .GroupBy(a => a.CreatedAt.Date)
            .Select(group => new
            {
                Date = group.Key,
                Count = group.Count()
            })
            .OrderByDescending(x => x.Date)
            .Take(14)
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken))
            .Select(x => new AuditLogTrendPointApiResponse(x.Date, x.Count))
            .ToList();

        return new AuditLogSummaryApiResponse(
            request.Entity,
            request.AuditAction,
            request.Search,
            request.StartDate,
            request.EndDate,
            request.IsSuccess,
            totalCount,
            successCount,
            failureCount,
            commandCount,
            failedCommandCount,
            topFailedActions,
            topFailedActors,
            failureTrend);
    }
}

/// <summary>
/// Defines the AuditLogApiQueryExtensions class used by the IT Form application.
/// </summary>
internal static class AuditLogApiQueryExtensions
{
    /// <summary>
    /// Runs the ToFilterParameters operation.
    /// </summary>
    public static AuditLogFilterParameters ToFilterParameters(this GetAuditLogListApiQuery request)
        => new(request.Entity, request.AuditAction, request.Search, request.StartDate, request.EndDate, request.IsSuccess);

    /// <summary>
    /// Runs the ToFilterParameters operation.
    /// </summary>
    public static AuditLogFilterParameters ToFilterParameters(this GetAuditLogSummaryApiQuery request)
        => new(request.Entity, request.AuditAction, request.Search, request.StartDate, request.EndDate, request.IsSuccess);
}
