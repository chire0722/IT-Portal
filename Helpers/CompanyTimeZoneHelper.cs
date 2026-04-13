using System.Globalization;
using IT_Form.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using NodaTime;
using NodaTime.TimeZones;

namespace IT_Form.Helpers;

public readonly record struct CompanyTimeZoneOption(string Id, string Label);

public static class CompanyTimeZoneHelper
{
    public const string DefaultTimeZoneId = "Asia/Kuala_Lumpur";

    private static readonly DateTimeZone DefaultTimeZone = DateTimeZoneProviders.Tzdb[DefaultTimeZoneId];
    private static readonly IReadOnlyList<CompanyTimeZoneOption> SupportedTimeZones = BuildSupportedTimeZones();
    private static readonly IReadOnlyDictionary<string, CompanyTimeZoneOption> SupportedTimeZonesById =
        SupportedTimeZones.ToDictionary(option => option.Id, option => option, StringComparer.OrdinalIgnoreCase);
    private static readonly IReadOnlyDictionary<string, string> WindowsToTzdbMap = BuildWindowsToTzdbMap();

    public static IReadOnlyList<CompanyTimeZoneOption> GetSupportedTimeZones() => SupportedTimeZones;

    public static IEnumerable<SelectListItem> GetSelectListItems(string? selectedTimeZoneId = null)
    {
        var selected = NormalizeTimeZoneId(selectedTimeZoneId);
        return SupportedTimeZones.Select(option => new SelectListItem
        {
            Value = option.Id,
            Text = option.Label,
            Selected = string.Equals(option.Id, selected, StringComparison.OrdinalIgnoreCase)
        });
    }

    public static bool IsSupportedTimeZoneId(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return false;
        }

        return SupportedTimeZonesById.ContainsKey(NormalizeTimeZoneId(timeZoneId));
    }

    public static string NormalizeTimeZoneId(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return DefaultTimeZoneId;
        }

        var trimmed = timeZoneId.Trim();
        if (SupportedTimeZonesById.TryGetValue(trimmed, out var supported))
        {
            return supported.Id;
        }

        if (WindowsToTzdbMap.TryGetValue(trimmed, out var tzdbId) &&
            SupportedTimeZonesById.TryGetValue(tzdbId, out var mappedZone))
        {
            return mappedZone.Id;
        }

        return DefaultTimeZoneId;
    }

    public static string GetDisplayName(string? timeZoneId)
    {
        var normalized = NormalizeTimeZoneId(timeZoneId);

        return SupportedTimeZonesById.TryGetValue(normalized, out var option)
            ? option.Label
            : normalized;
    }

    public static string ResolveTimeZoneId(IEnumerable<Company> companies, string? companyReference)
    {
        var company = CompanyBusinessHoursHelper.ResolveCompany(companies, companyReference);
        return NormalizeTimeZoneId(company?.TimeZoneId);
    }

    public static DateTimeZone ResolveTimeZone(string? timeZoneId)
    {
        var normalized = NormalizeTimeZoneId(timeZoneId);
        return DateTimeZoneProviders.Tzdb.GetZoneOrNull(normalized) ?? DefaultTimeZone;
    }

    public static DateTime ConvertFromUtc(DateTime utcDateTime, string? timeZoneId)
    {
        var normalizedUtc = EnsureUtc(utcDateTime);
        var instant = Instant.FromDateTimeUtc(normalizedUtc);
        return instant.InZone(ResolveTimeZone(timeZoneId)).ToDateTimeUnspecified();
    }

    public static DateTime ConvertToUtc(DateTime localDateTime, string? timeZoneId)
    {
        if (localDateTime.Kind == DateTimeKind.Utc)
        {
            return localDateTime;
        }

        var unspecifiedLocal = localDateTime.Kind == DateTimeKind.Unspecified
            ? localDateTime
            : DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);

        var local = LocalDateTime.FromDateTime(unspecifiedLocal);
        return ResolveTimeZone(timeZoneId)
            .AtLeniently(local)
            .ToInstant()
            .ToDateTimeUtc();
    }

    public static string FormatDateTime(DateTime? utcDateTime, string? timeZoneId, string format = "dd/MM/yyyy HH:mm")
    {
        if (!utcDateTime.HasValue)
        {
            return "-";
        }

        return ConvertFromUtc(utcDateTime.Value, timeZoneId)
            .ToString(format, CultureInfo.InvariantCulture);
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc)
        {
            return value;
        }

        if (value.Kind == DateTimeKind.Local)
        {
            return value.ToUniversalTime();
        }

        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private static IReadOnlyList<CompanyTimeZoneOption> BuildSupportedTimeZones()
    {
        var provider = DateTimeZoneProviders.Tzdb;
        var currentInstant = SystemClock.Instance.GetCurrentInstant();
        var options = provider.Ids
            .Select(provider.GetZoneOrNull)
            .Where(zone => zone is not null)
            .Select(zone =>
            {
                var actualZone = zone!;
                var offset = actualZone.GetUtcOffset(currentInstant).ToTimeSpan();
                return new
                {
                    actualZone.Id,
                    OffsetMinutes = offset.TotalMinutes,
                    Label = $"{FormatUtcOffset(offset)} {actualZone.Id}"
                };
            })
            .OrderBy(option => string.Equals(option.Id, DefaultTimeZoneId, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(option => option.OffsetMinutes)
            .ThenBy(option => option.Id, StringComparer.OrdinalIgnoreCase)
            .Select(option => new CompanyTimeZoneOption(option.Id, option.Label))
            .ToList();

        return options;
    }

    private static IReadOnlyDictionary<string, string> BuildWindowsToTzdbMap()
    {
        return TzdbDateTimeZoneSource.Default.WindowsMapping.PrimaryMapping
            .ToDictionary(mapping => mapping.Key, mapping => mapping.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static string FormatUtcOffset(TimeSpan offset)
    {
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        var absolute = offset.Duration();
        return $"(UTC{sign}{absolute:hh\\:mm})";
    }
}
