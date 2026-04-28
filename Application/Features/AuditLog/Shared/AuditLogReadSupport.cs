// Project comment: AuditLogReadSupport.cs is part of the IT Form application source code.

namespace IT_Form.Application.Features.AuditLogs.Shared;

/// <summary>
/// Defines the AuditLogFilterParameters record used by the IT Form application.
/// </summary>
public sealed record AuditLogFilterParameters(
    string? Entity,
    string? AuditAction,
    string? Search,
    DateTime? StartDate,
    DateTime? EndDate,
    bool? IsSuccess);

/// <summary>
/// Defines the AuditLogReadSupport class used by the IT Form application.
/// </summary>
public static class AuditLogReadSupport
{
    /// <summary>
    /// Applies changes for the ApplyFilters operation.
    /// </summary>
    public static IQueryable<IT_Form.Models.AuditLog> ApplyFilters(
        IQueryable<IT_Form.Models.AuditLog> query,
        AuditLogFilterParameters filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Entity))
        {
            query = query.Where(a => a.EntityName == filter.Entity);
        }

        if (!string.IsNullOrWhiteSpace(filter.AuditAction))
        {
            query = query.Where(a => a.Action == filter.AuditAction);
        }

        if (filter.IsSuccess.HasValue)
        {
            query = query.Where(a => a.IsSuccess == filter.IsSuccess.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var lowerSearch = filter.Search.ToLower();
            query = query.Where(a =>
                (a.UserId != null && a.UserId.ToLower().Contains(lowerSearch)) ||
                (a.PerformedBy != null && a.PerformedBy.ToLower().Contains(lowerSearch)) ||
                (a.EntityId != null && a.EntityId.ToLower().Contains(lowerSearch)) ||
                (a.IpAddress != null && a.IpAddress.ToLower().Contains(lowerSearch)) ||
                (a.Details != null && a.Details.ToLower().Contains(lowerSearch)) ||
                (a.ErrorMessage != null && a.ErrorMessage.ToLower().Contains(lowerSearch)) ||
                (a.Payload != null && a.Payload.ToLower().Contains(lowerSearch)) ||
                (a.Changes != null && a.Changes.ToLower().Contains(lowerSearch)));
        }

        if (filter.StartDate.HasValue)
        {
            var utcStart = filter.StartDate.Value.Date.ToUniversalTime();
            query = query.Where(a => a.CreatedAt >= utcStart);
        }

        if (filter.EndDate.HasValue)
        {
            var utcEnd = filter.EndDate.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
            query = query.Where(a => a.CreatedAt <= utcEnd);
        }

        return query;
    }
}
