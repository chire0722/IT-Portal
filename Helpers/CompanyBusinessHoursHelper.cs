using IT_Form.Models;

namespace IT_Form.Helpers;

public readonly record struct CompanyBusinessSchedule(
    TimeSpan StartTime,
    TimeSpan EndTime,
    HashSet<DayOfWeek> WorkingDays,
    string TimeZoneId)
{
    public static CompanyBusinessSchedule Default => new(
        SlaHelper.DefaultBusinessStartTime,
        SlaHelper.DefaultBusinessEndTime,
        SlaHelper.GetDefaultWorkingDays(),
        CompanyTimeZoneHelper.DefaultTimeZoneId);

    public double HoursPerDay => Math.Max((EndTime - StartTime).TotalHours, 0);

    public bool IsWorkingDay(DayOfWeek dayOfWeek) => WorkingDays.Contains(dayOfWeek);
}

public static class CompanyBusinessHoursHelper
{
    public static DateTime CalculateSlaDeadline(
        IEnumerable<Company> companies,
        string? companyReference,
        DateTime startUtc,
        int targetDays)
    {
        var businessSchedule = Resolve(companies, companyReference);
        return SlaHelper.CalculateSlaDeadline(
            startUtc,
            targetDays,
            businessSchedule.StartTime,
            businessSchedule.EndTime,
            businessSchedule.WorkingDays,
            businessSchedule.TimeZoneId);
    }

    public static bool IsSlaOverdue(
        IEnumerable<Company> companies,
        string? companyReference,
        DateTime startUtc,
        int targetDays)
    {
        var businessSchedule = Resolve(companies, companyReference);
        return SlaHelper.IsSlaOverdue(
            startUtc,
            targetDays,
            businessSchedule.StartTime,
            businessSchedule.EndTime,
            businessSchedule.WorkingDays,
            businessSchedule.TimeZoneId);
    }

    public static double CalculateBusinessHoursSpent(
        IEnumerable<Company> companies,
        string? companyReference,
        DateTime startUtc,
        DateTime endUtc)
    {
        var businessSchedule = Resolve(companies, companyReference);
        return SlaHelper.CalculateBusinessHoursSpent(
            startUtc,
            endUtc,
            businessSchedule.StartTime,
            businessSchedule.EndTime,
            businessSchedule.WorkingDays,
            businessSchedule.TimeZoneId);
    }

    public static CompanyBusinessSchedule Resolve(IEnumerable<Company> companies, string? companyReference)
    {
        var company = ResolveCompany(companies, companyReference);
        return company == null
            ? CompanyBusinessSchedule.Default
            : new CompanyBusinessSchedule(
                company.BusinessStartTime,
                company.BusinessEndTime,
                GetWorkingDays(company),
                CompanyTimeZoneHelper.NormalizeTimeZoneId(company.TimeZoneId));
    }

    public static string FormatWorkingDays(
        bool worksOnMonday,
        bool worksOnTuesday,
        bool worksOnWednesday,
        bool worksOnThursday,
        bool worksOnFriday,
        bool worksOnSaturday,
        bool worksOnSunday)
    {
        var labels = new List<string>();

        if (worksOnMonday) labels.Add("Mon");
        if (worksOnTuesday) labels.Add("Tue");
        if (worksOnWednesday) labels.Add("Wed");
        if (worksOnThursday) labels.Add("Thu");
        if (worksOnFriday) labels.Add("Fri");
        if (worksOnSaturday) labels.Add("Sat");
        if (worksOnSunday) labels.Add("Sun");

        return labels.Count > 0 ? string.Join(", ", labels) : "No business days";
    }

    public static Company? ResolveCompany(IEnumerable<Company> companies, string? companyReference)
    {
        if (string.IsNullOrWhiteSpace(companyReference))
        {
            return null;
        }

        var normalizedReference = companyReference.Trim();

        return companies.FirstOrDefault(company =>
                   string.Equals(company.DisplayName, normalizedReference, StringComparison.OrdinalIgnoreCase))
               ?? companies.FirstOrDefault(company =>
                   string.Equals(company.Name, normalizedReference, StringComparison.OrdinalIgnoreCase))
               ?? companies.FirstOrDefault(company =>
                   CompanyHelper.IsMatch(company.DisplayName, normalizedReference))
               ?? companies.FirstOrDefault(company =>
                   CompanyHelper.IsMatch(company.Name, normalizedReference));
    }

    private static HashSet<DayOfWeek> GetWorkingDays(Company company)
    {
        var workingDays = new HashSet<DayOfWeek>();

        if (company.WorksOnMonday) workingDays.Add(DayOfWeek.Monday);
        if (company.WorksOnTuesday) workingDays.Add(DayOfWeek.Tuesday);
        if (company.WorksOnWednesday) workingDays.Add(DayOfWeek.Wednesday);
        if (company.WorksOnThursday) workingDays.Add(DayOfWeek.Thursday);
        if (company.WorksOnFriday) workingDays.Add(DayOfWeek.Friday);
        if (company.WorksOnSaturday) workingDays.Add(DayOfWeek.Saturday);
        if (company.WorksOnSunday) workingDays.Add(DayOfWeek.Sunday);

        return workingDays.Count > 0 ? workingDays : SlaHelper.GetDefaultWorkingDays();
    }
}
